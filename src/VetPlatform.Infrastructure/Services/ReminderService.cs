using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Scheduling;
using VetPlatform.Domain.Entities.Notifications;
using VetPlatform.Domain.Entities.Scheduling;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Data;

namespace VetPlatform.Infrastructure.Services;

public class ReminderService(AppDbContext db, ICurrentUserService user, TimeProvider time) : IReminderService
{
    public const string AppointmentReminder = "AppointmentReminder";
    public const string CareReminder = "CareReminder";
    public static Notification Create(Guid owner, Guid relatedId, string type, string pet, string reason, DateTimeOffset at, DateTimeOffset now) => new()
    {
        RecipientUserId = owner, RelatedEntityId = relatedId, RelatedEntityType = type,
        Subject = "Recordatorio para tu mascota",
        Body = $"Mascota: {pet}. Motivo: {reason}. Fecha y hora: {at:yyyy-MM-dd HH:mm zzz}.",
        CreatedAt = now, NotBefore = at.AddHours(-24), EventAt = at
    };

    public async Task<Result<ReminderDto>> ScheduleCareAsync(ScheduleCareCommand command, CancellationToken ct = default)
    {
        var pet = await db.Pets.FindAsync(new object[] { command.PetId }, ct);
        if (pet == null || pet.IsDeleted) return Result<ReminderDto>.NotFound("Mascota no encontrada.");
        if (pet.OwnerId != user.UserId) return Result<ReminderDto>.Forbidden();
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Length > 500)
            return Result<ReminderDto>.Invalid("Indique el motivo del cuidado (máximo 500 caracteres).");
        if (command.DateTime <= time.GetUtcNow()) return Result<ReminderDto>.Invalid("El cuidado debe programarse para una fecha futura.");
        var notification = Create(pet.OwnerId, pet.Id, CareReminder, pet.Name, command.Reason.Trim(), command.DateTime.ToUniversalTime(), time.GetUtcNow());
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);
        return Result<ReminderDto>.Ok(ToDto(notification));
    }

    public async Task<Result<IReadOnlyList<ReminderDto>>> GetMineAsync(CancellationToken ct = default)
    {
        var items = await db.Notifications.AsNoTracking().Where(n => n.RecipientUserId == user.UserId &&
            (n.RelatedEntityType == AppointmentReminder || n.RelatedEntityType == CareReminder)).ToListAsync(ct);
        return Result<IReadOnlyList<ReminderDto>>.Ok(items.OrderBy(n => n.EventAt).Select(ToDto).ToList());
    }

    public async Task<Result<bool>> CancelCareAsync(Guid id, CancellationToken ct = default)
    {
        var item = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.RelatedEntityType == CareReminder, ct);
        if (item == null) return Result<bool>.NotFound("Cuidado no encontrado.");
        if (item.RecipientUserId != user.UserId) return Result<bool>.Forbidden();
        if (item.Status != NotificationStatus.Sent) item.Status = NotificationStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> CancelAppointmentAsync(Guid id, CancellationToken ct = default)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (appointment == null) return Result<bool>.NotFound("Cita no encontrada.");
        if (appointment.OwnerId != user.UserId && appointment.VetUserId != user.UserId && !user.IsInRole(UserRole.PlatformAdmin))
            return Result<bool>.Forbidden();
        if (appointment.Status == Appointment.Statuses.Cancelled) return Result<bool>.Ok(true);
        appointment.Status = Appointment.Statuses.Cancelled;
        var slot = await db.AvailabilitySlots.FirstOrDefaultAsync(s => s.Id == appointment.SlotId, ct);
        if (slot != null) { slot.IsBooked = false; slot.Version++; }
        var reminders = await db.Notifications.Where(n => n.RelatedEntityId == id && n.RelatedEntityType == AppointmentReminder && n.Status != NotificationStatus.Sent).ToListAsync(ct);
        foreach (var reminder in reminders) reminder.Status = NotificationStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }

    private static ReminderDto ToDto(Notification n) => new(n.Id, n.Subject, n.Body, n.EventAt, n.Status.ToString());
}
