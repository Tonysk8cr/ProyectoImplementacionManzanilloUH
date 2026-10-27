using FluentValidation;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Entities.Clinical;
using VetPlatform.Domain.Enums;
using VetPlatform.Domain.Interfaces.Repositories;

namespace VetPlatform.Infrastructure.Services;

public class ClinicalService : IClinicalService
{
    private readonly IPetRepository _pets;
    private readonly IClinicalRecordRepository _records;
    private readonly IConsultationRepository _consultations;
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IEmergencyAttendanceRepository _emergencyAttendances;
    private readonly ClinicalAccessGuard _guard;
    private readonly IAuditService _audit;
    private readonly INotificationService _notifications;
    private readonly ICurrentUserService _user;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;
    private readonly IValidator<CreatePetCommand> _petValidator;
    private readonly IValidator<RegisterConsultationCommand> _consultationValidator;
    private readonly IValidator<RegisterPrescriptionCommand> _prescriptionValidator;

    public ClinicalService(IPetRepository pets, IClinicalRecordRepository records, IConsultationRepository consultations,
        IPrescriptionRepository prescriptions, IEmergencyAttendanceRepository emergencyAttendances, ClinicalAccessGuard guard,
        IAuditService audit, INotificationService notifications, ICurrentUserService user, IUnitOfWork uow, TimeProvider time,
        IValidator<CreatePetCommand> petValidator, IValidator<RegisterConsultationCommand> consultationValidator,
        IValidator<RegisterPrescriptionCommand> prescriptionValidator)
    {
        _pets = pets; _records = records; _consultations = consultations; _prescriptions = prescriptions;
        _emergencyAttendances = emergencyAttendances; _guard = guard; _audit = audit; _notifications = notifications;
        _user = user; _uow = uow; _time = time; _petValidator = petValidator; _consultationValidator = consultationValidator;
        _prescriptionValidator = prescriptionValidator;
    }

    private Guid Actor => _user.UserId ?? Guid.Empty;

    private async Task<Result<T>> DenyAsync<T>(string resourceType, Guid resourceId, AuditAction action = AuditAction.AccessDenied, CancellationToken ct = default)
    {
        await _audit.LogAsync(Actor, action, resourceType, resourceId.ToString(), _user.OrganizationId, AuditLog.Results.Denied, null, ct);
        return Result<T>.Forbidden("No tiene autorización sobre este expediente.");
    }

    public async Task<Result<PetDto>> CreatePetAsync(CreatePetCommand cmd, CancellationToken ct = default)
    {
        if (_user.UserId is not { } actor || !_user.IsInRole(UserRole.PetOwner) || cmd.OwnerId != actor)
            return Result<PetDto>.Forbidden("Solo el tutor puede registrar sus mascotas.");
        var v = await _petValidator.ValidateAsync(cmd, ct);
        if (!v.IsValid) return Result<PetDto>.Invalid(v.Errors.Select(e => e.ErrorMessage).ToArray());

        var now = _time.GetUtcNow();
        var pet = new Pet { OwnerId = actor, Name = cmd.Name.Trim(), Species = cmd.Species.Trim(), Breed = cmd.Breed, BirthDate = cmd.BirthDate,
            QrCode = Pet.GenerateOpaqueQrCode(), CreatedAt = now };
        await _pets.AddAsync(pet, ct);
        await _records.AddAsync(new ClinicalRecord { PetId = pet.Id, CreatedAt = now }, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(actor, AuditAction.Create, "Pet", pet.Id.ToString(), null, AuditLog.Results.Allowed, null, ct);
        return Result<PetDto>.Ok(pet.ToDto());
    }

    public async Task<Result<PetDto>> GetPetAsync(Guid petId, CancellationToken ct = default)
    {
        var pet = await _pets.GetByIdAsync(petId, ct);
        if (pet == null) return Result<PetDto>.NotFound("Mascota no encontrada.");
        if (await _guard.GetReadAccessAsync(pet, Actor, _user.Roles, ct) == ClinicalAccessLevel.None) return await DenyAsync<PetDto>("Pet", petId, ct: ct);
        await _audit.LogAsync(Actor, AuditAction.Read, "Pet", petId.ToString(), _user.OrganizationId, AuditLog.Results.Allowed, null, ct);
        return Result<PetDto>.Ok(pet.ToDto());
    }

    public async Task<Result<ClinicalRecordDto>> GetHistoryAsync(Guid petId, CancellationToken ct = default)
    {
        var pet = await _pets.GetByIdAsync(petId, ct);
        if (pet == null) return Result<ClinicalRecordDto>.NotFound("Mascota no encontrada.");
        var level = await _guard.GetReadAccessAsync(pet, Actor, _user.Roles, ct);
        if (level == ClinicalAccessLevel.None) return await DenyAsync<ClinicalRecordDto>("ClinicalRecord", petId, ct: ct);
        var record = await _records.GetFullHistoryAsync(petId, ct);
        if (record == null) return Result<ClinicalRecordDto>.NotFound("Expediente no encontrado.");
        var emergencies = await _emergencyAttendances.GetByClinicalRecordAsync(record.Id, ct);
        await _audit.LogAsync(Actor, AuditAction.Read, "ClinicalRecord", record.Id.ToString(), _user.OrganizationId, AuditLog.Results.Allowed, $"via={level}", ct);
        return Result<ClinicalRecordDto>.Ok(new ClinicalRecordDto(record.Id, record.PetId, record.CreatedAt,
            record.Consultations.OrderByDescending(c => c.AttendanceDate).Select(c => c.ToDto()).ToList(),
            emergencies.OrderByDescending(e => e.AttendanceDate).Select(e => new EmergencyAttendanceSummaryDto(e.Id, e.VetUserId, e.AttendanceDate, e.Findings, e.Actions, e.Instructions)).ToList()));
    }

    public async Task<Result<ConsultationDto>> RegisterConsultationAsync(RegisterConsultationCommand cmd, CancellationToken ct = default)
    {
        var v = await _consultationValidator.ValidateAsync(cmd, ct);
        if (!v.IsValid) return Result<ConsultationDto>.Invalid(v.Errors.Select(e => e.ErrorMessage).ToArray());
        var record = await _records.GetByIdAsync(cmd.ClinicalRecordId, ct);
        if (record == null) return Result<ConsultationDto>.NotFound("Expediente no encontrado.");
        if (cmd.VetUserId != Actor || !await _guard.CanWriteAsAttendingVetAsync(record.PetId, Actor, _user.Roles, ct))
            return await DenyAsync<ConsultationDto>("Consultation", record.Id, ct: ct);

        var c = new Consultation
        {
            ClinicalRecordId = record.Id, VetUserId = Actor, AttendanceDate = cmd.AttendanceDate, RegistrationDate = _time.GetUtcNow(),
            Notes = cmd.Notes, Instructions = cmd.Instructions, Status = ConsultationStatus.Confirmed, SourceType = Consultation.SourceTypes.Routine,
        };
        await _consultations.AddAsync(c, ct);
        var pet = await _pets.GetByIdAsync(record.PetId, ct);
        if (pet != null)
            await _notifications.EnqueueAsync(pet.OwnerId, "Nueva consulta registrada", $"Se registró una consulta para {pet.Name}.", c.Id, "Consultation", ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(Actor, AuditAction.Create, "Consultation", c.Id.ToString(), _user.OrganizationId, AuditLog.Results.Allowed, null, ct);
        return Result<ConsultationDto>.Ok(c.ToDto());
    }

    public async Task<Result<ConsultationDto>> GetConsultationAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _consultations.GetWithDetailsAsync(id, ct);
        if (c == null) return Result<ConsultationDto>.NotFound("Consulta no encontrada.");
        var record = await _records.GetByIdAsync(c.ClinicalRecordId, ct);
        var pet = record == null ? null : await _pets.GetByIdAsync(record.PetId, ct);
        if (pet == null || await _guard.GetReadAccessAsync(pet, Actor, _user.Roles, ct) == ClinicalAccessLevel.None)
            return await DenyAsync<ConsultationDto>("Consultation", id, ct: ct);
        await _audit.LogAsync(Actor, AuditAction.Read, "Consultation", id.ToString(), _user.OrganizationId, AuditLog.Results.Allowed, null, ct);
        return Result<ConsultationDto>.Ok(c.ToDto());
    }

    public async Task<Result<AddendumDto>> AddAddendumAsync(AddAddendumCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Reason) || string.IsNullOrWhiteSpace(cmd.Content))
            return Result<AddendumDto>.Invalid("Motivo y contenido de la adenda son obligatorios.");
        var c = await _consultations.GetByIdAsync(cmd.ConsultationId, ct);
        if (c == null) return Result<AddendumDto>.NotFound("Consulta no encontrada.");
        var record = await _records.GetByIdAsync(c.ClinicalRecordId, ct);
        if (record == null || !await _guard.CanWriteAsAttendingVetAsync(record.PetId, Actor, _user.Roles, ct))
            return await DenyAsync<AddendumDto>("Consultation", c.Id, ct: ct);

        // Correction = new addendum. The original consultation is never modified.
        var a = new Addendum { ConsultationId = c.Id, AuthorUserId = Actor, Reason = cmd.Reason, Content = cmd.Content, CreatedAt = _time.GetUtcNow() };
        await _consultations.AddAddendumAsync(a, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(Actor, AuditAction.Create, "Addendum", a.Id.ToString(), _user.OrganizationId, AuditLog.Results.Allowed, $"consultation={c.Id}", ct);
        return Result<AddendumDto>.Ok(a.ToDto());
    }

    public async Task<Result<PrescriptionDto>> RegisterPrescriptionAsync(RegisterPrescriptionCommand cmd, CancellationToken ct = default)
    {
        var v = await _prescriptionValidator.ValidateAsync(cmd, ct);
        if (!v.IsValid) return Result<PrescriptionDto>.Invalid(v.Errors.Select(e => e.ErrorMessage).ToArray());
        var c = await _consultations.GetByIdAsync(cmd.ConsultationId, ct);
        if (c == null) return Result<PrescriptionDto>.NotFound("Consulta no encontrada.");
        var record = await _records.GetByIdAsync(c.ClinicalRecordId, ct);
        if (record == null || !await _guard.CanWriteAsAttendingVetAsync(record.PetId, Actor, _user.Roles, ct))
            return await DenyAsync<PrescriptionDto>("Prescription", c.Id, ct: ct);

        var p = new Prescription { ConsultationId = c.Id, VetUserId = Actor, Medication = cmd.Medication, Dosage = cmd.Dosage, Route = cmd.Route,
            Frequency = cmd.Frequency, DurationDays = cmd.DurationDays, CreatedAt = _time.GetUtcNow() };
        await _prescriptions.AddAsync(p, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(Actor, AuditAction.Create, "Prescription", p.Id.ToString(), _user.OrganizationId, AuditLog.Results.Allowed, null, ct);
        return Result<PrescriptionDto>.Ok(p.ToDto());
    }

    public async Task<Result<PrescriptionDto>> GetPrescriptionAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _prescriptions.GetWithAddendaAsync(id, ct);
        if (p == null) return Result<PrescriptionDto>.NotFound("Receta no encontrada.");
        var c = await _consultations.GetByIdAsync(p.ConsultationId, ct);
        var record = c == null ? null : await _records.GetByIdAsync(c.ClinicalRecordId, ct);
        var pet = record == null ? null : await _pets.GetByIdAsync(record.PetId, ct);
        if (pet == null || await _guard.GetReadAccessAsync(pet, Actor, _user.Roles, ct) == ClinicalAccessLevel.None)
            return await DenyAsync<PrescriptionDto>("Prescription", id, ct: ct);
        await _audit.LogAsync(Actor, AuditAction.Read, "Prescription", id.ToString(), _user.OrganizationId, AuditLog.Results.Allowed, null, ct);
        return Result<PrescriptionDto>.Ok(p.ToDto());
    }

    public async Task<Result<AddendumDto>> AddPrescriptionAddendumAsync(AddPrescriptionAddendumCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Reason) || string.IsNullOrWhiteSpace(cmd.Content))
            return Result<AddendumDto>.Invalid("Motivo y contenido de la adenda son obligatorios.");
        var p = await _prescriptions.GetByIdAsync(cmd.PrescriptionId, ct);
        if (p == null) return Result<AddendumDto>.NotFound("Receta no encontrada.");
        var c = await _consultations.GetByIdAsync(p.ConsultationId, ct);
        var record = c == null ? null : await _records.GetByIdAsync(c.ClinicalRecordId, ct);
        if (record == null || !await _guard.CanWriteAsAttendingVetAsync(record.PetId, Actor, _user.Roles, ct))
            return await DenyAsync<AddendumDto>("Prescription", p.Id, ct: ct);
        var a = new PrescriptionAddendum { PrescriptionId = p.Id, AuthorUserId = Actor, Reason = cmd.Reason, Content = cmd.Content, CreatedAt = _time.GetUtcNow() };
        await _prescriptions.AddAddendumAsync(a, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(Actor, AuditAction.Create, "PrescriptionAddendum", a.Id.ToString(), _user.OrganizationId, AuditLog.Results.Allowed, null, ct);
        return Result<AddendumDto>.Ok(a.ToDto());
    }
}
