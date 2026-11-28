using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Scheduling;
using VetPlatform.Domain.Entities.Scheduling;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Data;
using VetPlatform.Infrastructure.Services;
using Xunit;

namespace VetPlatform.Application.Tests;

public class ClinicAppointmentServiceTests : IDisposable
{
    private readonly TestFixture _f = new();
    public void Dispose() => _f.Dispose();

    private async Task<Guid> Book(ClinicalScenario s)
    {
        _f.CurrentUser.As(s.AdminId, UserRole.PlatformAdmin);
        using var scope = _f.NewScope();
        var svc = _f.Get<ISchedulingService>(scope);
        var slot = await svc.CreateSlotAsync(new(s.ClinicId, s.VetId, _f.Clock.Now.AddHours(24), _f.Clock.Now.AddHours(25)));
        _f.CurrentUser.As(s.OwnerId, UserRole.PetOwner);
        var booked = await svc.BookAppointmentAsync(new(s.PetId, slot.Value!.Id, "Vacunas"));
        booked.Success.Should().BeTrue();
        return booked.Value!.Id;
    }

    [Fact]
    public async Task Owner_and_staff_from_another_clinic_cannot_read_or_change_appointments()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var id = await Book(s);
        using var scope = _f.NewScope();
        var svc = _f.Get<IClinicAppointmentService>(scope);
        (await svc.GetClinicsAsync()).ErrorType.Should().Be(ErrorType.Forbidden);
        (await svc.SearchAsync(s.ClinicId, null, null)).ErrorType.Should().Be(ErrorType.Forbidden);
        (await svc.GetAsync(id)).ErrorType.Should().Be(ErrorType.Forbidden);
        (await svc.ChangeStatusAsync(id, Appointment.Statuses.Cancelled)).ErrorType.Should().Be(ErrorType.Forbidden);
        var foreign = await _f.SeedUserAsync(UserRole.AttendingVet);
        _f.CurrentUser.As(foreign, UserRole.AttendingVet);
        (await svc.GetClinicsAsync()).Value.Should().BeEmpty();
        (await svc.SearchAsync(s.ClinicId, null, null)).ErrorType.Should().Be(ErrorType.Forbidden);
        (await svc.GetAsync(id)).ErrorType.Should().Be(ErrorType.NotFound);
        (await svc.ChangeStatusAsync(id, Appointment.Statuses.Cancelled)).ErrorType.Should().Be(ErrorType.NotFound);
        (await _f.Get<AppDbContext>(scope).Appointments.SingleAsync(a => a.Id == id)).Status.Should().Be(Appointment.Statuses.Booked);
    }

    [Fact]
    public async Task Active_member_can_filter_by_local_date_and_state_and_read_details()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var id = await Book(s);
        _f.CurrentUser.As(s.VetId, UserRole.AttendingVet);
        using var scope = _f.NewScope();
        var svc = _f.Get<IClinicAppointmentService>(scope);
        // Put the appointment on the following UTC day, while it is still the selected local day.
        var db = _f.Get<AppDbContext>(scope);
        var slot = await db.AvailabilitySlots.SingleAsync();
        slot.StartTime = new DateTimeOffset(2026, 10, 28, 5, 30, 0, TimeSpan.Zero);
        slot.EndTime = slot.StartTime.AddMinutes(30);
        await db.SaveChangesAsync();
        var localDate = new DateOnly(2026, 10, 27);
        var items = await svc.SearchAsync(s.ClinicId, localDate, Appointment.Statuses.Booked);
        items.Success.Should().BeTrue();
        items.Value.Should().ContainSingle(a => a.Id == id && a.Pet == "Firulais" && a.Reason == "Vacunas" && a.StartTime.Hour == 23);
        items.Value![0].Client.Should().NotBeNullOrEmpty();
        (await svc.SearchAsync(s.ClinicId, localDate.AddDays(1), null)).Value.Should().BeEmpty();
        (await svc.SearchAsync(s.ClinicId, localDate, Appointment.Statuses.Completed)).Value.Should().BeEmpty();
        (await svc.GetAsync(id)).Value!.AllowedStatuses.Should().Equal(Appointment.Statuses.Cancelled);
        (await svc.SearchAsync(s.ClinicId, null, "invalid")).ErrorType.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData(Appointment.Statuses.Cancelled)]
    [InlineData(Appointment.Statuses.Completed)]
    [InlineData(Appointment.Statuses.NoShow)]
    public async Task Valid_transition_persists_refreshes_list_and_detail_and_suppresses_reminder(string state)
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var id = await Book(s);
        _f.Clock.Advance(TimeSpan.FromHours(25));
        _f.CurrentUser.As(s.VetId, UserRole.AttendingVet);
        using (var scope = _f.NewScope())
        {
            var svc = _f.Get<IClinicAppointmentService>(scope);
            (await svc.ChangeStatusAsync(id, state)).Success.Should().BeTrue();
            (await svc.ChangeStatusAsync(id, Appointment.Statuses.Cancelled)).ErrorType.Should().Be(ErrorType.Conflict);
        }
        using var fresh = _f.NewScope();
        var persisted = _f.Get<IClinicAppointmentService>(fresh);
        (await persisted.GetAsync(id)).Value!.Status.Should().Be(state);
        (await persisted.GetAsync(id)).Value!.AllowedStatuses.Should().BeEmpty();
        (await persisted.SearchAsync(s.ClinicId, null, state)).Value.Should().ContainSingle(a => a.Id == id);
        var db = _f.Get<AppDbContext>(fresh);
        (await db.Notifications.SingleAsync(n => n.RelatedEntityId == id && n.RelatedEntityType == ReminderService.AppointmentReminder)).Status.Should().Be(NotificationStatus.Cancelled);
        (await db.AvailabilitySlots.SingleAsync()).IsBooked.Should().Be(state != Appointment.Statuses.Cancelled);
        (await db.AuditLogs.Where(a => a.ResourceId == id.ToString() && a.Action == AuditAction.Update).CountAsync()).Should().Be(1);
        // The owner's existing cancellation API must not reopen a finished appointment.
        if (state != Appointment.Statuses.Cancelled)
        {
            _f.CurrentUser.As(s.OwnerId, UserRole.PetOwner);
            (await _f.Get<IReminderService>(fresh).CancelAppointmentAsync(id)).ErrorType.Should().Be(ErrorType.Conflict);
        }
    }

    [Fact]
    public async Task Future_appointment_cannot_be_completed_or_reopened_and_revoked_membership_blocks_access()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var id = await Book(s);
        _f.CurrentUser.As(s.VetId, UserRole.AttendingVet);
        using var scope = _f.NewScope();
        var svc = _f.Get<IClinicAppointmentService>(scope);
        (await svc.ChangeStatusAsync(id, Appointment.Statuses.Completed)).ErrorType.Should().Be(ErrorType.Conflict);
        (await svc.ChangeStatusAsync(id, Appointment.Statuses.NoShow)).ErrorType.Should().Be(ErrorType.Conflict);
        (await svc.ChangeStatusAsync(id, Appointment.Statuses.Booked)).ErrorType.Should().Be(ErrorType.Validation);
        var db = _f.Get<AppDbContext>(scope);
        (await db.ClinicMembers.SingleAsync()).LeftAt = _f.Clock.Now;
        await db.SaveChangesAsync();
        (await svc.GetAsync(id)).ErrorType.Should().Be(ErrorType.NotFound);
        (await svc.ChangeStatusAsync(id, Appointment.Statuses.Cancelled)).ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Concurrent_state_change_is_rejected_without_overwriting_the_saved_status()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var id = await Book(s);
        _f.Clock.Advance(TimeSpan.FromHours(25));
        _f.CurrentUser.As(s.VetId, UserRole.AttendingVet);
        using var stale = _f.NewScope();
        var staleDb = _f.Get<AppDbContext>(stale);
        await staleDb.Appointments.SingleAsync(a => a.Id == id);
        await staleDb.AvailabilitySlots.SingleAsync();
        using (var other = _f.NewScope())
            (await _f.Get<IClinicAppointmentService>(other).ChangeStatusAsync(id, Appointment.Statuses.Cancelled)).Success.Should().BeTrue();
        (await _f.Get<IClinicAppointmentService>(stale).ChangeStatusAsync(id, Appointment.Statuses.Completed)).ErrorType.Should().Be(ErrorType.Conflict);
        using var check = _f.NewScope();
        (await _f.Get<AppDbContext>(check).Appointments.SingleAsync(a => a.Id == id)).Status.Should().Be(Appointment.Statuses.Cancelled);
        (await _f.Get<AppDbContext>(check).AvailabilitySlots.SingleAsync()).IsBooked.Should().BeFalse();
    }

    [Fact]
    public async Task Clinic_admin_access_uses_database_assignment_without_requiring_cookie_org_claims()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var id = await Book(s);
        var admin = await _f.SeedUserAsync(UserRole.ClinicAdmin, orgId: s.ClinicId);
        _f.CurrentUser.As(admin, UserRole.ClinicAdmin);
        using var scope = _f.NewScope();
        var svc = _f.Get<IClinicAppointmentService>(scope);
        (await svc.GetClinicsAsync()).Value.Should().ContainSingle(c => c.Id == s.ClinicId);
        (await svc.ChangeStatusAsync(id, Appointment.Statuses.Cancelled)).Success.Should().BeTrue();
    }
}
