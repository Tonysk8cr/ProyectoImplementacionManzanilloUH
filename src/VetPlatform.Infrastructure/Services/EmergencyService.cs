using Microsoft.Extensions.Options;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Application.Features.Emergency;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Entities.Emergency;
using VetPlatform.Domain.Enums;
using VetPlatform.Domain.Interfaces.Repositories;

namespace VetPlatform.Infrastructure.Services;

public class EmergencyAccessOptions
{
    public int DefaultExpiryMinutes { get; set; } = 120;
}

public class EmergencyService : IEmergencyService
{
    private readonly IPetRepository _pets;
    private readonly IUserRepository _users;
    private readonly IClinicalRecordRepository _records;
    private readonly IEmergencyAccessRepository _accesses;
    private readonly IEmergencyAttendanceRepository _attendances;
    private readonly IAttendingVetRelationshipRepository _relationships;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;
    private readonly EmergencyAccessOptions _opt;

    public EmergencyService(IPetRepository pets, IUserRepository users, IClinicalRecordRepository records, IEmergencyAccessRepository accesses,
        IEmergencyAttendanceRepository attendances, IAttendingVetRelationshipRepository relationships, INotificationService notifications,
        IAuditService audit, ICurrentUserService user, IUnitOfWork uow, TimeProvider time, IOptions<EmergencyAccessOptions> opt)
    {
        _pets = pets; _users = users; _records = records; _accesses = accesses; _attendances = attendances; _relationships = relationships;
        _notifications = notifications; _audit = audit; _user = user; _uow = uow; _time = time; _opt = opt.Value;
    }

    private Guid Actor => _user.UserId ?? Guid.Empty;

    public async Task<Result<PetSummaryForEmergencyDto>> ResolvePetAsync(string qrOrId, CancellationToken ct = default)
    {
        if (_user.UserId == null) return Result<PetSummaryForEmergencyDto>.Fail(ErrorType.Unauthorized, "Autenticación requerida.");
        var pet = await _pets.GetByQrCodeAsync(qrOrId, ct) ?? (Guid.TryParse(qrOrId, out var id) ? await _pets.GetByIdAsync(id, ct) : null);
        if (pet == null) return Result<PetSummaryForEmergencyDto>.NotFound("Mascota no encontrada.");
        await _audit.LogAsync(Actor, AuditAction.Read, "PetEmergencySummary", pet.Id.ToString(), null, AuditLog.Results.Allowed, null, ct);
        var hasVet = await _relationships.GetActiveForPetAsync(pet.Id, ct) != null;
        return Result<PetSummaryForEmergencyDto>.Ok(new PetSummaryForEmergencyDto(pet.Id, pet.Name, pet.Species, pet.Breed, hasVet));
    }

    public async Task<Result<EmergencyAccessDto>> RequestAccessAsync(RequestEmergencyAccessCommand cmd, CancellationToken ct = default)
    {
        if (cmd.VetUserId != Actor || !_user.IsInRole(UserRole.EmergencyVet))
            return await DenyAsync<EmergencyAccessDto>(cmd.PetId, "role", ct);
        var vet = await _users.GetByIdAsync(Actor, ct);
        if (vet == null || !vet.IsVerified || vet.IsSuspended) return await DenyAsync<EmergencyAccessDto>(cmd.PetId, "unverified", ct);
        if (string.IsNullOrWhiteSpace(cmd.Reason) || cmd.Reason.Trim().Length < 10)
            return Result<EmergencyAccessDto>.Invalid("Debe justificar el acceso urgente (mínimo 10 caracteres).");
        var pet = await _pets.GetByIdAsync(cmd.PetId, ct);
        if (pet == null) return Result<EmergencyAccessDto>.NotFound("Mascota no encontrada.");

        var now = _time.GetUtcNow();
        var access = new EmergencyAccess { PetId = pet.Id, VetUserId = Actor, Reason = cmd.Reason.Trim(), GrantedAt = now,
            ExpiresAt = now.AddMinutes(_opt.DefaultExpiryMinutes), Status = EmergencyAccessStatus.Active };
        await _accesses.AddAsync(access, ct);
        await _notifications.EnqueueAsync(pet.OwnerId, "Acceso urgente al expediente",
            $"Un veterinario de urgencias accedió al expediente de {pet.Name} hasta {access.ExpiresAt:u}.", access.Id, "EmergencyAccess", ct);
        await _uow.SaveChangesAsync(ct);
        // Justification is stored in the access entity; audit keeps only metadata.
        await _audit.LogAsync(Actor, AuditAction.AccessGranted, "EmergencyAccess", access.Id.ToString(), null, AuditLog.Results.Allowed, $"pet={pet.Id}", ct);
        return Result<EmergencyAccessDto>.Ok(access.ToDto());
    }

    public async Task<Result<EmergencyAttendanceDto>> RegisterAttendanceAsync(RegisterEmergencyAttendanceCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Findings) || string.IsNullOrWhiteSpace(cmd.Actions))
            return Result<EmergencyAttendanceDto>.Invalid("Hallazgos y acciones son obligatorios.");
        var access = await _accesses.GetByIdAsync(cmd.EmergencyAccessId, ct);
        if (access == null) return Result<EmergencyAttendanceDto>.NotFound("Acceso urgente no encontrado.");
        if (access.VetUserId != Actor || !_user.IsInRole(UserRole.EmergencyVet))
            return await DenyAsync<EmergencyAttendanceDto>(access.PetId, "not-owner-of-access", ct);
        // Access state is re-checked on every clinical operation.
        if (!access.IsValidAt(_time.GetUtcNow()))
        {
            await _uow.SaveChangesAsync(ct);
            return await DenyAsync<EmergencyAttendanceDto>(access.PetId, $"access-{access.Status}", ct);
        }
        var record = await _records.GetByPetIdAsync(access.PetId, ct);
        var pet = await _pets.GetByIdAsync(access.PetId, ct);
        if (record == null || pet == null) return Result<EmergencyAttendanceDto>.NotFound("Expediente no encontrado.");

        var now = _time.GetUtcNow();
        var att = new EmergencyAttendance { EmergencyAccessId = access.Id, ClinicalRecordId = record.Id, VetUserId = Actor, AttendanceDate = cmd.AttendanceDate,
            RegistrationDate = now, Findings = cmd.Findings, Actions = cmd.Actions, Instructions = cmd.Instructions, CreatedAt = now };
        await _attendances.AddAsync(att, ct);
        // Notifications staged in the same SaveChanges; dispatched asynchronously.
        await _notifications.EnqueueAsync(pet.OwnerId, "Atención de urgencia registrada", $"Se registró una atención de urgencia para {pet.Name}.", att.Id, "EmergencyAttendance", ct);
        var regente = await _relationships.GetActiveForPetAsync(pet.Id, ct);
        if (regente != null)
            await _notifications.EnqueueAsync(regente.VetUserId, "Atención de urgencia a su paciente", $"{pet.Name} recibió atención de urgencia.", att.Id, "EmergencyAttendance", ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(Actor, AuditAction.Create, "EmergencyAttendance", att.Id.ToString(), null, AuditLog.Results.Allowed, $"access={access.Id}", ct);
        return Result<EmergencyAttendanceDto>.Ok(att.ToDto());
    }

    private async Task<Result<T>> DenyAsync<T>(Guid petId, string why, CancellationToken ct)
    {
        await _audit.LogAsync(Actor, AuditAction.AccessDenied, "EmergencyAccess", petId.ToString(), null, AuditLog.Results.Denied, why, ct);
        return Result<T>.Forbidden("Acceso urgente no autorizado o no vigente.");
    }
}
