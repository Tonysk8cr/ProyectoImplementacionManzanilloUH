using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Application.Features.Scheduling;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Entities.Organizations;
using VetPlatform.Domain.Entities.Scheduling;
using VetPlatform.Domain.Enums;
using VetPlatform.Domain.Interfaces.Repositories;

namespace VetPlatform.Infrastructure.Services;

public class SchedulingService : ISchedulingService
{
    private readonly IAvailabilitySlotRepository _slots;
    private readonly IAppointmentRepository _appointments;
    private readonly IClinicRepository _clinics;
    private readonly IPetRepository _pets;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;
    private readonly IValidator<BookAppointmentCommand> _bookValidator;

    public SchedulingService(IAvailabilitySlotRepository slots, IAppointmentRepository appointments, IClinicRepository clinics, IPetRepository pets,
        INotificationService notifications, IAuditService audit, ICurrentUserService user, IUnitOfWork uow, TimeProvider time, IValidator<BookAppointmentCommand> bookValidator)
    {
        _slots = slots;
        _appointments = appointments;
        _clinics = clinics;
        _pets = pets;
        _notifications = notifications;
        _audit = audit;
        _user = user;
        _uow = uow;
        _time = time;
        _bookValidator = bookValidator;
    }

    public async Task<Result<AvailabilitySlotDto>> CreateSlotAsync(CreateAvailabilitySlotCommand cmd, CancellationToken ct = default)
    {
        var clinic = await _clinics.GetByIdAsync(cmd.ClinicId, ct);
        if (clinic == null) return Result<AvailabilitySlotDto>.NotFound("Clínica no encontrada.");
        if (clinic.Status != ClinicStatus.Active) return Result<AvailabilitySlotDto>.Conflict("La clínica no está activa.");
        var isAdmin = _user.IsInRole(UserRole.PlatformAdmin) || clinic.RepresentativeUserId == _user.UserId ||
                      (_user.IsInRole(UserRole.ClinicAdmin) && _user.OrganizationIds.Contains(clinic.Id));
        if (!isAdmin) return Result<AvailabilitySlotDto>.Forbidden();
        if (cmd.EndTime <= cmd.StartTime) return Result<AvailabilitySlotDto>.Invalid("La hora de fin debe ser posterior al inicio.");
        if (cmd.StartTime <= _time.GetUtcNow()) return Result<AvailabilitySlotDto>.Invalid("La disponibilidad debe ser futura.");
        if (await _clinics.GetActiveMemberAsync(clinic.Id, cmd.VetUserId, ct) == null)
            return Result<AvailabilitySlotDto>.Invalid("El veterinario no es integrante activo de la clínica.");
        if (await _slots.HasOverlapAsync(cmd.VetUserId, cmd.StartTime, cmd.EndTime, ct))
            return Result<AvailabilitySlotDto>.Conflict("El horario se solapa con otra disponibilidad del veterinario.");

        var slot = new AvailabilitySlot
        {
            ClinicId = clinic.Id,
            VetUserId = cmd.VetUserId,
            StartTime = cmd.StartTime,
            EndTime = cmd.EndTime
        };
        await _slots.AddAsync(slot, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<AvailabilitySlotDto>.Ok(slot.ToDto());
    }

    public async Task<Result<IReadOnlyList<AvailabilitySlotDto>>> GetAvailabilityAsync(Guid? clinicId, Guid? vetId, DateOnly? date, CancellationToken ct = default)
    {
        var now = _time.GetUtcNow();
        var slots = await _slots.SearchAsync(clinicId, vetId, date, onlyFree: true, ct);
        return Result<IReadOnlyList<AvailabilitySlotDto>>.Ok(slots
            .Where(s => s.StartTime > now)
            .Select(s => s.ToDto())
            .ToList());
    }

    public async Task<Result<AppointmentDto>> BookAppointmentAsync(BookAppointmentCommand cmd, CancellationToken ct = default)
    {
        var validation = await _bookValidator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
            return Result<AppointmentDto>.Invalid(validation.Errors.Select(e => e.ErrorMessage).ToArray());
        if (_user.UserId is not { } actor || !_user.IsInRole(UserRole.PetOwner))
            return Result<AppointmentDto>.Forbidden("Solo un propietario puede reservar una cita.");

        var pet = await _pets.GetByIdAsync(cmd.PetId, ct);
        if (pet == null) return Result<AppointmentDto>.NotFound("Mascota no encontrada.");
        if (pet.OwnerId != actor) return Result<AppointmentDto>.Forbidden("Solo el tutor puede reservar citas para su mascota.");

        try
        {
            return await _uow.ExecuteInTransactionAsync(async () =>
            {
                var slot = await _slots.GetByIdAsync(cmd.SlotId, ct);
                if (slot == null)
                    return Result<AppointmentDto>.Conflict("El horario ya no está disponible. Actualice la disponibilidad y seleccione otro horario.");
                if (slot.ClinicId != cmd.ClinicId)
                    return Result<AppointmentDto>.Conflict("La veterinaria seleccionada no corresponde al horario.");
                if (slot.IsBooked || await _appointments.ExistsActiveForSlotAsync(slot.Id, ct))
                    return Result<AppointmentDto>.Conflict("El horario ya no está disponible. Actualice la disponibilidad y seleccione otro horario.");
                if (slot.StartTime <= _time.GetUtcNow())
                    return Result<AppointmentDto>.Conflict("El horario ya pasó. Actualice la disponibilidad y seleccione otro horario.");

                var clinic = await _clinics.GetByIdAsync(slot.ClinicId, ct);
                if (clinic == null || clinic.Status != ClinicStatus.Active ||
                    await _clinics.GetActiveMemberAsync(slot.ClinicId, slot.VetUserId, ct) == null)
                    return Result<AppointmentDto>.Conflict("La agenda de la veterinaria cambió. Actualice la disponibilidad y seleccione otro horario.");

                var appointment = new Appointment
                {
                    PetId = pet.Id,
                    OwnerId = pet.OwnerId,
                    ClinicId = slot.ClinicId,
                    VetUserId = slot.VetUserId,
                    SlotId = slot.Id,
                    CreatedAt = _time.GetUtcNow(),
                    Reason = cmd.Reason.Trim(),
                    Status = Appointment.Statuses.Booked
                };
                slot.IsBooked = true;
                slot.Version++;
                await _appointments.AddAsync(appointment, ct);
                await _notifications.EnqueueAsync(slot.VetUserId, "Nueva cita", $"Cita para {pet.Name} el {slot.StartTime:u}.", appointment.Id, "Appointment", ct);
                await _notifications.EnqueueAsync(pet.OwnerId, "Cita confirmada", $"Cita confirmada para {pet.Name} el {slot.StartTime:u}.", appointment.Id, "Appointment", ct);
                await _uow.SaveChangesAsync(ct);
                await _audit.LogAsync(pet.OwnerId, AuditAction.Create, "Appointment", appointment.Id.ToString(), slot.ClinicId, AuditLog.Results.Allowed, null, ct);

                return Result<AppointmentDto>.Ok(ToDto(appointment, pet.Name, clinic.Name, slot));
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<AppointmentDto>.Conflict("El horario acaba de ser reservado. Actualice la disponibilidad y seleccione otro horario.");
        }
        catch (DbUpdateException exception) when (IsActiveSlotUniqueViolation(exception))
        {
            return Result<AppointmentDto>.Conflict("El horario acaba de ser reservado. Actualice la disponibilidad y seleccione otro horario.");
        }
    }

    public async Task<Result<IReadOnlyList<AppointmentDto>>> GetAppointmentsByPetAsync(Guid petId, CancellationToken ct = default)
    {
        var pet = await _pets.GetByIdAsync(petId, ct);
        if (pet == null) return Result<IReadOnlyList<AppointmentDto>>.NotFound("Mascota no encontrada.");
        var details = await _appointments.SearchDetailsAsync(petId: petId, ct: ct);
        if (pet.OwnerId != _user.UserId && !details.Any(a => a.VetUserId == _user.UserId))
            return Result<IReadOnlyList<AppointmentDto>>.Forbidden();
        return Result<IReadOnlyList<AppointmentDto>>.Ok(details.Select(ToDto).ToList());
    }

    public async Task<Result<AppointmentDto>> GetAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var details = await _appointments.SearchDetailsAsync(appointmentId: appointmentId, ct: ct);
        var appointment = details.SingleOrDefault();
        if (appointment == null) return Result<AppointmentDto>.NotFound("Cita no encontrada.");

        var isOwner = appointment.OwnerId == _user.UserId;
        var isVet = appointment.VetUserId == _user.UserId;
        if (!isOwner && !isVet)
        {
            var clinic = await _clinics.GetByIdAsync(appointment.ClinicId, ct);
            if (clinic == null || !IsClinicAdmin(clinic))
                return Result<AppointmentDto>.Forbidden();
        }
        return Result<AppointmentDto>.Ok(ToDto(appointment));
    }

    public async Task<Result<IReadOnlyList<AppointmentDto>>> GetAppointmentsForCurrentUserAsync(Guid? clinicId = null, CancellationToken ct = default)
    {
        if (_user.UserId is not { } actor)
            return Result<IReadOnlyList<AppointmentDto>>.Fail(ErrorType.Unauthorized, "Debe iniciar sesión.");

        if (clinicId.HasValue)
        {
            var clinic = await _clinics.GetByIdAsync(clinicId.Value, ct);
            if (clinic == null) return Result<IReadOnlyList<AppointmentDto>>.NotFound("Clínica no encontrada.");
            if (IsClinicAdmin(clinic))
                return await GetAppointmentDetailsAsync(clinicId: clinic.Id, ct: ct);
            if (_user.IsInRole(UserRole.PetOwner))
                return await GetAppointmentDetailsAsync(ownerId: actor, clinicId: clinic.Id, ct: ct);
            if (_user.IsInRole(UserRole.AttendingVet) &&
                await _clinics.GetActiveMemberAsync(clinic.Id, actor, ct) != null)
                return await GetAppointmentDetailsAsync(vetUserId: actor, clinicId: clinic.Id, ct: ct);
            return Result<IReadOnlyList<AppointmentDto>>.Forbidden();
        }

        if (_user.IsInRole(UserRole.PetOwner))
            return await GetAppointmentDetailsAsync(ownerId: actor, ct: ct);
        if (_user.IsInRole(UserRole.AttendingVet))
            return await GetAppointmentDetailsAsync(vetUserId: actor, ct: ct);
        if (_user.IsInRole(UserRole.ClinicAdmin) || _user.IsInRole(UserRole.PlatformAdmin))
        {
            var clinics = await _clinics.GetManagedByUserAsync(
                actor, _user.OrganizationIds, _user.IsInRole(UserRole.PlatformAdmin), ct);
            return await GetAppointmentDetailsAsync(clinicIds: clinics.Select(c => c.Id).ToList(), ct: ct);
        }
        return Result<IReadOnlyList<AppointmentDto>>.Forbidden();
    }

    private async Task<Result<IReadOnlyList<AppointmentDto>>> GetAppointmentDetailsAsync(
        Guid? ownerId = null,
        Guid? vetUserId = null,
        Guid? clinicId = null,
        IReadOnlyCollection<Guid>? clinicIds = null,
        CancellationToken ct = default)
    {
        var details = await _appointments.SearchDetailsAsync(
            ownerId: ownerId, vetUserId: vetUserId, clinicId: clinicId, clinicIds: clinicIds, ct: ct);
        return Result<IReadOnlyList<AppointmentDto>>.Ok(details.Select(ToDto).ToList());
    }

    private bool IsClinicAdmin(Clinic clinic) =>
        _user.IsInRole(UserRole.PlatformAdmin) ||
        clinic.RepresentativeUserId == _user.UserId ||
        (_user.IsInRole(UserRole.ClinicAdmin) && _user.OrganizationIds.Contains(clinic.Id));

    private static bool IsActiveSlotUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Appointments_SlotId"
        };

    private static AppointmentDto ToDto(Appointment appointment, string petName, string clinicName, AvailabilitySlot slot) =>
        new(appointment.Id, appointment.PetId, petName, appointment.OwnerId, appointment.ClinicId, clinicName,
            appointment.VetUserId, appointment.SlotId, slot.StartTime, slot.EndTime, appointment.CreatedAt,
            appointment.Reason, appointment.Status);

    private static AppointmentDto ToDto(AppointmentDetails appointment) =>
        new(appointment.Id, appointment.PetId, appointment.PetName, appointment.OwnerId, appointment.ClinicId,
            appointment.ClinicName, appointment.VetUserId, appointment.SlotId, appointment.StartTime,
            appointment.EndTime, appointment.CreatedAt, appointment.Reason, appointment.Status);
}
