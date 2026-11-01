using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Application.Features.Emergency;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Data;
using Xunit;

namespace VetPlatform.Application.Tests;

public class EmergencyServiceTests : IDisposable
{
    private readonly TestFixture _f = new();
    public void Dispose() => _f.Dispose();
    private static readonly UserRole[] Er = { UserRole.EmergencyVet };

    [Fact]
    public async Task Resolve_by_qr_returns_only_minimal_summary()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var er = await _f.SeedUserAsync(UserRole.EmergencyVet);
        var r = await _f.RunAs<IEmergencyService, Result<PetSummaryForEmergencyDto>>(er, Er, svc => svc.ResolvePetAsync(s.Pet.QrCode));
        r.Success.Should().BeTrue();
        r.Value!.PetId.Should().Be(s.PetId);
        r.Value.HasAttendingVet.Should().BeTrue();
    }

    [Fact]
    public async Task Verified_emergency_vet_gets_temporary_access_and_owner_is_notified()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var er = await _f.SeedUserAsync(UserRole.EmergencyVet);
        var access = await _f.RunAs<IEmergencyService, Result<EmergencyAccessDto>>(er, Er, svc =>
            svc.RequestAccessAsync(new RequestEmergencyAccessCommand(s.PetId, er, "Paciente politraumatizado ingresado")));
        access.Success.Should().BeTrue();
        access.Value!.ExpiresAt.Should().Be(_f.Clock.Now.AddMinutes(120));

        var history = await _f.RunAs<IClinicalService, Result<ClinicalRecordDto>>(er, Er, svc => svc.GetHistoryAsync(s.PetId));
        history.Success.Should().BeTrue();

        using var scope = _f.NewScope();
        (await _f.Get<AppDbContext>(scope).Notifications.AnyAsync(n => n.RecipientUserId == s.OwnerId && n.RelatedEntityType == "EmergencyAccess")).Should().BeTrue();
    }

    [Fact]
    public async Task Unverified_emergency_vet_is_denied()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var er = await _f.SeedUserAsync(UserRole.EmergencyVet, verified: false);
        var r = await _f.RunAs<IEmergencyService, Result<EmergencyAccessDto>>(er, Er, svc =>
            svc.RequestAccessAsync(new RequestEmergencyAccessCommand(s.PetId, er, "Paciente politraumatizado ingresado")));
        r.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Expired_access_blocks_reading_and_attendance_registration()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var er = await _f.SeedUserAsync(UserRole.EmergencyVet);
        var access = await _f.RunAs<IEmergencyService, Result<EmergencyAccessDto>>(er, Er, svc =>
            svc.RequestAccessAsync(new RequestEmergencyAccessCommand(s.PetId, er, "Intoxicación aguda sospechada")));
        _f.Clock.Advance(TimeSpan.FromMinutes(121));

        var att = await _f.RunAs<IEmergencyService, Result<EmergencyAttendanceDto>>(er, Er, svc =>
            svc.RegisterAttendanceAsync(new RegisterEmergencyAttendanceCommand(access.Value!.Id, _f.Clock.Now, "Vómitos", "Lavado gástrico", null)));
        att.ErrorType.Should().Be(ErrorType.Forbidden);

        var read = await _f.RunAs<IClinicalService, Result<ClinicalRecordDto>>(er, Er, svc => svc.GetHistoryAsync(s.PetId));
        read.ErrorType.Should().Be(ErrorType.Forbidden);

        using var scope = _f.NewScope();
        (await _f.Get<AppDbContext>(scope).EmergencyAccesses.SingleAsync(a => a.Id == access.Value!.Id)).Status.Should().Be(EmergencyAccessStatus.Expired);
    }

    [Fact]
    public async Task Attendance_is_registered_with_notifications_to_owner_and_regente_then_dispatched()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var er = await _f.SeedUserAsync(UserRole.EmergencyVet);
        var access = await _f.RunAs<IEmergencyService, Result<EmergencyAccessDto>>(er, Er, svc =>
            svc.RequestAccessAsync(new RequestEmergencyAccessCommand(s.PetId, er, "Atropellamiento, requiere historial")));
        var att = await _f.RunAs<IEmergencyService, Result<EmergencyAttendanceDto>>(er, Er, svc =>
            svc.RegisterAttendanceAsync(new RegisterEmergencyAttendanceCommand(access.Value!.Id, _f.Clock.Now, "Fractura", "Inmovilización", "Control con regente")));
        att.Success.Should().BeTrue(string.Join(",", att.Errors));

        using (var scope = _f.NewScope())
        {
            var db = _f.Get<AppDbContext>(scope);
            var notes = await db.Notifications.Where(n => n.RelatedEntityId == att.Value!.Id).ToListAsync();
            notes.Select(n => n.RecipientUserId).Should().BeEquivalentTo(new[] { s.OwnerId, s.VetId });
            notes.Should().OnlyContain(n => n.Status == NotificationStatus.Pending);
            // Audit has no clinical content.
            (await db.AuditLogs.AnyAsync(a => a.Context != null && a.Context.Contains("Fractura"))).Should().BeFalse();
        }

        using (var scope = _f.NewScope())
        {
            var sent = await _f.Get<INotificationService>(scope).RetryPendingNotificationsAsync();
            sent.Should().BeGreaterThanOrEqualTo(2);
        }
        using (var scope = _f.NewScope())
            (await _f.Get<AppDbContext>(scope).Notifications.CountAsync(n => n.Status == NotificationStatus.Pending)).Should().Be(0);
    }
}
