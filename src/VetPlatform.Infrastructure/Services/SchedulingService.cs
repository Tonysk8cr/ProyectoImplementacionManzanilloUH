using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Application.Features.Scheduling;
using VetPlatform.Domain.Entities.Audit;
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
        _slots = slots; _appointments = appointments; _clinics = clinics; _pets = pets; _notifications = notifications;
        _audit = audit; _user = user; _uow = uow; _time = time; _bookValidator = bookValidator;
    }

    public async Task<Result<AvailabilitySlotDto>> CreateSlotAsync(CreateAvailabilitySlotCommand cmd, CancellationToken ct = default)
    {
        var clinic = await _clinics.GetByIdAsync(cmd.ClinicId, ct);
        if (clinic == null) return Result<AvailabilitySlotDto>.NotFound("Clínica no encontrada.");
        var isAdmin = _user.IsInRole(UserRole.PlatformAdmin) || clinic.RepresentativeUserId == _user.UserId ||
                      (_user.IsInRole(UserRole.ClinicAdmin) && _user.OrganizationIds.Contains(clinic.Id));
        if (!isAdmin) return Result<AvailabilitySlotDto>.Forbidden();
        if (cmd.EndTime <= cmd.StartTime) return Result<AvailabilitySlotDto>.Invalid("La hora de fin debe ser posterior al inicio.");
        if (cmd.StartTime <= _time.GetUtcNow()) return Result<AvailabilitySlotDto>.Invalid("La disponibilidad debe ser futura.");
        if (await _clinics.GetActiveMemberAsync(clinic.Id, cmd.VetUserId, ct) == null)
            return Result<AvailabilitySlotDto>.Invalid("El veterinario no es integrante activo de la clínica.");
        if (await _slots.HasOverlapAsync(cmd.VetUserId, cmd.StartTime, cmd.EndTime, ct))
            return Result<AvailabilitySlotDto>.Conflict("El horario se solapa con otra disponibilidad del veterinario.");
        var slot = new AvailabilitySlot { ClinicId = clinic.Id, VetUserId = cmd.VetUserId, StartTime = cmd.StartTime, EndTime = cmd.EndTime };
        await _slots.AddAsync(slot, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<AvailabilitySlotDto>.Ok(slot.ToDto());
    }

    public async Task<Result<IReadOnlyList<AvailabilitySlotDto>>> GetAvailabilityAsync(Guid? clinicId, Guid? vetId, DateOnly? date, CancellationToken ct = default)
    {
        var now = _time.GetUtcNow();
        var slots = await _slots.SearchAsync(clinicId, vetId, date, onlyFree: true, ct);
        return Result<IReadOnlyList<AvailabilitySlotDto>>.Ok(slots.Where(s => s.StartTime > now).Select(s => s.ToDto()).ToList());
    }

    public async Task<Result<AppointmentDto>> BookAppointmentAsync(BookAppointmentCommand cmd, CancellationToken ct = default)
    {
        var v = await _bookValidator.ValidateAsync(cmd, ct);
        if (!v.IsValid) return Result<AppointmentDto>.Invalid(v.Errors.Select(e => e.ErrorMessage).ToArray());
        var pet = await _pets.GetByIdAsync(cmd.PetId, ct);
        if (pet == null) return Result<AppointmentDto>.NotFound("Mascota no encontrada.");
        if (pet.OwnerId != _user.UserId) return Result<AppointmentDto>.Forbidden("Solo el tutor puede reservar citas para su mascota.");

        try
        {
            return await _uow.ExecuteInTransactionAsync(async () =>
            {
                // Availability is validated at confirmation time, not only at query time.
                var slot = await _slots.GetByIdAsync(cmd.SlotId, ct);
                if (slot == null) return Result<AppointmentDto>.NotFound("Horario no encontrado.");
                if (slot.IsBooked || await _appointments.ExistsActiveForSlotAsync(slot.Id, ct))
                    return Result<AppointmentDto>.Conflict("El horario ya no está disponible.");
                if (slot.StartTime <= _time.GetUtcNow()) return Result<AppointmentDto>.Conflict("El horario ya pasó.");
                if (await _clinics.GetActiveMemberAsync(slot.ClinicId, slot.VetUserId, ct) == null)
                    return Result<AppointmentDto>.Conflict("El veterinario ya no está disponible en esta clínica.");

                slot.IsBooked = true;
                slot.Version++; // optimistic concurrency token
                var appt = new Appointment { PetId = pet.Id, OwnerId = pet.OwnerId, ClinicId = slot.ClinicId, VetUserId = slot.VetUserId,
                    SlotId = slot.Id, CreatedAt = _time.GetUtcNow(), Status = Appointment.Statuses.Booked };
                await _appointments.AddAsync(appt, ct);
                await _notifications.EnqueueAsync(slot.VetUserId, "Nueva cita", $"Cita para {pet.Name} el {slot.StartTime:u}.", appt.Id, "Appointment", ct);
                await _notifications.EnqueueAsync(pet.OwnerId, "Cita confirmada", $"Cita confirmada para {pet.Name} el {slot.StartTime:u}.", appt.Id, "Appointment", ct);
                await _uow.SaveChangesAsync(ct);
                await _audit.LogAsync(pet.OwnerId, AuditAction.Create, "Appointment", appt.Id.ToString(), slot.ClinicId, AuditLog.Results.Allowed, null, ct);
                return Result<AppointmentDto>.Ok(appt.ToDto());
            }, ct);
        }
        catch (DbUpdateException)
        {
            return Result<AppointmentDto>.Conflict("El horario fue reservado por otro usuario.");
        }
    }

    public async Task<Result<IReadOnlyList<AppointmentDto>>> GetAppointmentsByPetAsync(Guid petId, CancellationToken ct = default)
    {
        var pet = await _pets.GetByIdAsync(petId, ct);
        if (pet == null) return Result<IReadOnlyList<AppointmentDto>>.NotFound("Mascota no encontrada.");
        var list = await _appointments.GetByPetAsync(petId, ct);
        if (pet.OwnerId != _user.UserId && !list.Any(a => a.VetUserId == _user.UserId))
            return Result<IReadOnlyList<AppointmentDto>>.Forbidden();
        return Result<IReadOnlyList<AppointmentDto>>.Ok(list.OrderBy(a => a.CreatedAt).Select(a => a.ToDto()).ToList());
    }
}
