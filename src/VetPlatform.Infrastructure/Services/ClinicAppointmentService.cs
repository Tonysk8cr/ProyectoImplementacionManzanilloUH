using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Application.Features.Scheduling;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Entities.Scheduling;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Data;

namespace VetPlatform.Infrastructure.Services;

public class ClinicAppointmentService(AppDbContext db, ICurrentUserService user, TimeProvider time,
    IAuditService audit, IUnitOfWork uow) : IClinicAppointmentService
{
    private static readonly TimeSpan CostaRicaOffset = TimeSpan.FromHours(-6);
    private static readonly string[] States = [Appointment.Statuses.Booked, Appointment.Statuses.Cancelled, Appointment.Statuses.Completed, Appointment.Statuses.NoShow];

    private bool IsStaff => user.UserId.HasValue && (user.IsInRole(UserRole.PlatformAdmin) || user.IsInRole(UserRole.ClinicAdmin) ||
        user.IsInRole(UserRole.AttendingVet) || user.IsInRole(UserRole.EmergencyVet) || user.IsInRole(UserRole.ExternalVet));

    private IQueryable<VetPlatform.Domain.Entities.Organizations.Clinic> Clinics()
    {
        var query = db.Clinics.AsNoTracking();
        if (!IsStaff) return query.Where(c => false);
        if (user.IsInRole(UserRole.PlatformAdmin)) return query;
        var admin = user.IsInRole(UserRole.ClinicAdmin);
        return query.Where(c => c.RepresentativeUserId == user.UserId ||
            (admin && db.UserRoleAssignments.Any(r => r.UserId == user.UserId && r.Role == UserRole.ClinicAdmin && r.OrganizationId == c.Id && r.RevokedAt == null)) ||
            db.ClinicMembers.Any(m => m.ClinicId == c.Id && m.UserId == user.UserId && m.LeftAt == null));
    }

    public async Task<Result<IReadOnlyList<ClinicOptionDto>>> GetClinicsAsync(CancellationToken ct = default)
    {
        if (!IsStaff) return Result<IReadOnlyList<ClinicOptionDto>>.Forbidden();
        return Result<IReadOnlyList<ClinicOptionDto>>.Ok(await Clinics().OrderBy(c => c.Name).Select(c => new ClinicOptionDto(c.Id, c.Name)).ToListAsync(ct));
    }

    private IQueryable<ClinicAppointmentDto> Query() =>
        from a in db.Appointments.AsNoTracking()
        join c in Clinics() on a.ClinicId equals c.Id
        join p in db.Pets on a.PetId equals p.Id
        join owner in db.Users on a.OwnerId equals owner.Id
        join vet in db.Users on a.VetUserId equals vet.Id
        join slot in db.AvailabilitySlots on a.SlotId equals slot.Id
        select new ClinicAppointmentDto
        {
            Id = a.Id, ClinicId = a.ClinicId, Clinic = c.Name, PetId = p.Id, Pet = p.Name, Client = owner.FullName,
            Vet = vet.FullName, StartTime = slot.StartTime, EndTime = slot.EndTime, Reason = a.Reason, Status = a.Status
        };

    private IReadOnlyList<string> Actions(string status, DateTimeOffset start)
    {
        if (status != Appointment.Statuses.Booked) return Array.Empty<string>();
        return start <= time.GetUtcNow()
            ? [Appointment.Statuses.Cancelled, Appointment.Statuses.Completed, Appointment.Statuses.NoShow]
            : [Appointment.Statuses.Cancelled];
    }

    private ClinicAppointmentDto Localize(ClinicAppointmentDto item) => item with
    {
        StartTime = item.StartTime.ToOffset(CostaRicaOffset), EndTime = item.EndTime.ToOffset(CostaRicaOffset),
        AllowedStatuses = Actions(item.Status, item.StartTime)
    };

    public async Task<Result<IReadOnlyList<ClinicAppointmentDto>>> SearchAsync(Guid clinicId, DateOnly? date, string? status, CancellationToken ct = default)
    {
        if (!IsStaff || !await Clinics().AnyAsync(c => c.Id == clinicId, ct)) return Result<IReadOnlyList<ClinicAppointmentDto>>.Forbidden();
        if (!string.IsNullOrEmpty(status) && !States.Contains(status)) return Result<IReadOnlyList<ClinicAppointmentDto>>.Invalid("Estado de cita inválido.");
        var query = Query().Where(a => a.ClinicId == clinicId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(a => a.Status == status);
        if (date.HasValue && db.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var from = new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MinValue), CostaRicaOffset).ToUniversalTime();
            var to = from.AddDays(1);
            query = query.Where(a => a.StartTime >= from && a.StartTime < to);
        }
        var list = await query.ToListAsync(ct);
        var localized = list.Select(Localize).Where(a => date == null || DateOnly.FromDateTime(a.StartTime.DateTime) == date)
            .OrderBy(a => a.StartTime).ThenBy(a => a.Id).ToList();
        return Result<IReadOnlyList<ClinicAppointmentDto>>.Ok(localized);
    }

    public async Task<Result<ClinicAppointmentDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        if (!IsStaff) return Result<ClinicAppointmentDto>.Forbidden();
        var item = await Query().FirstOrDefaultAsync(a => a.Id == id, ct);
        return item == null ? Result<ClinicAppointmentDto>.NotFound("Cita no encontrada o sin acceso.") : Result<ClinicAppointmentDto>.Ok(Localize(item));
    }

    public async Task<Result<bool>> ChangeStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        if (!IsStaff) return Result<bool>.Forbidden();
        if (!States.Contains(status) || status == Appointment.Statuses.Booked) return Result<bool>.Invalid("Acción no permitida.");
        try
        {
            return await uow.ExecuteInTransactionAsync(async () =>
            {
                var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);
                if (appointment == null || !await Clinics().AnyAsync(c => c.Id == appointment.ClinicId, ct))
                    return Result<bool>.NotFound("Cita no encontrada o sin acceso.");
                var slot = await db.AvailabilitySlots.FirstOrDefaultAsync(s => s.Id == appointment.SlotId, ct);
                if (slot == null || !Actions(appointment.Status, slot.StartTime).Contains(status))
                    return Result<bool>.Conflict("La acción ya no está disponible para esta cita. Actualiza la página.");
                var previous = appointment.Status;
                appointment.Status = status;
                // Uses the slot's existing concurrency token to serialize competing state changes and reservations.
                slot.Version++;
                if (status == Appointment.Statuses.Cancelled) slot.IsBooked = false;
                var reminders = await db.Notifications.Where(n => n.RelatedEntityId == id && n.RelatedEntityType == ReminderService.AppointmentReminder &&
                    (n.Status == NotificationStatus.Pending || n.Status == NotificationStatus.Failed)).ToListAsync(ct);
                foreach (var reminder in reminders) reminder.Status = NotificationStatus.Cancelled;
                await db.SaveChangesAsync(ct);
                await audit.LogAsync(user.UserId!.Value, AuditAction.Update, "Appointment", id.ToString(), appointment.ClinicId,
                    AuditLog.Results.Allowed, $"status:{previous}->{status}", ct);
                return Result<bool>.Ok(true);
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            uow.DiscardUnsavedChanges();
            return Result<bool>.Conflict("La cita fue modificada por otra persona. Actualiza la página.");
        }
    }
}
