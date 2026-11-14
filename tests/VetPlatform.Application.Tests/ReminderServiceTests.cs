using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Scheduling;
using VetPlatform.Domain.Entities.Notifications;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Data;
using VetPlatform.Infrastructure.Services;
using Xunit;

namespace VetPlatform.Application.Tests;

public class ReminderServiceTests : IDisposable
{
    private readonly TestFixture _f = new();
    public void Dispose() => _f.Dispose();

    private async Task<Notification> Book(ClinicalScenario s)
    {
        _f.CurrentUser.As(s.AdminId, UserRole.PlatformAdmin);
        using var scope = _f.NewScope();
        var scheduling = _f.Get<ISchedulingService>(scope);
        var slot = await scheduling.CreateSlotAsync(new(s.ClinicId, s.VetId, _f.Clock.Now.AddHours(48), _f.Clock.Now.AddHours(49)));
        _f.CurrentUser.As(s.OwnerId, UserRole.PetOwner);
        var booked = await scheduling.BookAppointmentAsync(new(s.PetId, slot.Value!.Id, "Vacunación"));
        booked.Success.Should().BeTrue();
        return await _f.Get<AppDbContext>(scope).Notifications.SingleAsync(n => n.RelatedEntityType == ReminderService.AppointmentReminder);
    }

    private async Task Dispatch()
    {
        using var scope = _f.NewScope();
        await _f.Get<INotificationService>(scope).RetryPendingNotificationsAsync();
    }

    private async Task<Notification> Read(Guid id)
    {
        using var scope = _f.NewScope();
        return await _f.Get<AppDbContext>(scope).Notifications.SingleAsync(n => n.Id == id);
    }

    [Fact]
    public async Task Appointment_reminder_is_sent_24_hours_before_and_only_once()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var reminder = await Book(s);
        reminder.Body.Should().Contain("Vacunación").And.Contain("Fecha y hora:").And.Contain("Mascota:");
        await Dispatch();
        (await Read(reminder.Id)).Status.Should().Be(NotificationStatus.Pending);
        _f.Clock.Advance(TimeSpan.FromHours(24));
        await Dispatch();
        var sent = await Read(reminder.Id);
        sent.Status.Should().Be(NotificationStatus.Sent);
        _f.Clock.Advance(TimeSpan.FromMinutes(1));
        await Dispatch();
        (await Read(reminder.Id)).SentAt.Should().Be(sent.SentAt);
    }

    [Fact]
    public async Task Cancelled_appointment_does_not_send_reminder()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var reminder = await Book(s);
        using (var scope = _f.NewScope())
            (await _f.Get<IReminderService>(scope).CancelAppointmentAsync(reminder.RelatedEntityId!.Value)).Success.Should().BeTrue();
        _f.Clock.Advance(TimeSpan.FromHours(24));
        await Dispatch();
        (await Read(reminder.Id)).Status.Should().Be(NotificationStatus.Cancelled);
    }

    [Fact]
    public async Task Dispatch_rechecks_appointment_status_even_if_cancelled_elsewhere()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var reminder = await Book(s);
        using (var scope = _f.NewScope())
        {
            var db = _f.Get<AppDbContext>(scope);
            (await db.Appointments.SingleAsync()).Status = "Cancelled";
            await db.SaveChangesAsync();
        }
        _f.Clock.Advance(TimeSpan.FromHours(24));
        await Dispatch();
        (await Read(reminder.Id)).Status.Should().Be(NotificationStatus.Cancelled);
    }

    [Fact]
    public async Task Care_requires_owner_and_future_date_and_can_be_cancelled()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        using var scope = _f.NewScope();
        var service = _f.Get<IReminderService>(scope);
        _f.CurrentUser.As(s.VetId, UserRole.AttendingVet);
        (await service.ScheduleCareAsync(new(s.PetId, "Baño", _f.Clock.Now.AddHours(48)))).ErrorType.Should().Be(ErrorType.Forbidden);
        _f.CurrentUser.As(s.OwnerId, UserRole.PetOwner);
        (await service.ScheduleCareAsync(new(s.PetId, "Baño", _f.Clock.Now))).ErrorType.Should().Be(ErrorType.Validation);
        var care = await service.ScheduleCareAsync(new(s.PetId, "Baño", _f.Clock.Now.AddHours(48)));
        care.Success.Should().BeTrue();
        (await service.CancelCareAsync(care.Value!.Id)).Success.Should().BeTrue();
        _f.Clock.Advance(TimeSpan.FromHours(24));
        await Dispatch();
        (await Read(care.Value.Id)).Status.Should().Be(NotificationStatus.Cancelled);
    }

    [Theory]
    [InlineData(1, NotificationStatus.Sent)]
    [InlineData(3, NotificationStatus.Cancelled)]
    public async Task Care_inside_reminder_window_is_sent_but_expired_care_is_suppressed(int hours, NotificationStatus expected)
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        _f.CurrentUser.As(s.OwnerId, UserRole.PetOwner);
        Guid id;
        using (var scope = _f.NewScope())
            id = (await _f.Get<IReminderService>(scope).ScheduleCareAsync(new(s.PetId, "Medicamento", _f.Clock.Now.AddHours(2)))).Value!.Id;
        _f.Clock.Advance(TimeSpan.FromHours(hours));
        await Dispatch();
        (await Read(id)).Status.Should().Be(expected);
    }
}
