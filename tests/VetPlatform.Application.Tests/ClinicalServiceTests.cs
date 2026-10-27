using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Application.Features.Organizations;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Data;
using Xunit;

namespace VetPlatform.Application.Tests;

public class ClinicalServiceTests : IDisposable
{
    private readonly TestFixture _f = new();
    public void Dispose() => _f.Dispose();
    private static readonly UserRole[] Vet = { UserRole.AttendingVet };

    [Fact]
    public async Task Pet_gets_opaque_qr_code_different_from_id()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        s.Pet.QrCode.Should().NotBeNullOrEmpty().And.NotBe(s.PetId.ToString()).And.NotBe(s.PetId.ToString("N"));
    }

    [Fact]
    public async Task Attending_vet_registers_consultation_and_owner_is_notified()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var r = await _f.RunAs<IClinicalService, Result<ConsultationDto>>(s.VetId, Vet, svc =>
            svc.RegisterConsultationAsync(new RegisterConsultationCommand(s.RecordId, s.VetId, _f.Clock.Now.AddHours(-1), "Revisión general", "Dieta blanda")));

        r.Success.Should().BeTrue(string.Join(",", r.Errors));
        r.Value!.Status.Should().Be("Confirmed");
        using var scope = _f.NewScope();
        var db = _f.Get<AppDbContext>(scope);
        (await db.Notifications.CountAsync(n => n.RecipientUserId == s.OwnerId && n.Status == NotificationStatus.Pending)).Should().BeGreaterThan(0);
        (await db.AuditLogs.AnyAsync(a => a.ResourceType == "Consultation" && a.Action == AuditAction.Create)).Should().BeTrue();
    }

    [Fact]
    public async Task Addendum_is_appended_and_original_consultation_is_unchanged()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var c = await _f.RunAs<IClinicalService, Result<ConsultationDto>>(s.VetId, Vet, svc =>
            svc.RegisterConsultationAsync(new RegisterConsultationCommand(s.RecordId, s.VetId, _f.Clock.Now, "Nota original", null)));
        var a = await _f.RunAs<IClinicalService, Result<AddendumDto>>(s.VetId, Vet, svc =>
            svc.AddAddendumAsync(new AddAddendumCommand(c.Value!.Id, "Error de transcripción", "Peso correcto: 12kg")));
        a.Success.Should().BeTrue();

        var reloaded = await _f.RunAs<IClinicalService, Result<ConsultationDto>>(s.VetId, Vet, svc => svc.GetConsultationAsync(c.Value!.Id));
        reloaded.Value!.Notes.Should().Be("Nota original");
        reloaded.Value.Addenda.Should().ContainSingle(x => x.Content == "Peso correcto: 12kg");
    }

    [Fact]
    public async Task Modifying_a_confirmed_consultation_is_blocked_at_persistence_level()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var c = await _f.RunAs<IClinicalService, Result<ConsultationDto>>(s.VetId, Vet, svc =>
            svc.RegisterConsultationAsync(new RegisterConsultationCommand(s.RecordId, s.VetId, _f.Clock.Now, "Original", null)));
        using var scope = _f.NewScope();
        var db = _f.Get<AppDbContext>(scope);
        var entity = await db.Consultations.SingleAsync(x => x.Id == c.Value!.Id);
        entity.Notes = "Sobrescrito";
        var act = () => db.SaveChangesAsync();
        await act.Should().ThrowAsync<VetPlatform.Domain.Exceptions.BusinessRuleViolationException>();
    }

    [Fact]
    public async Task Vet_without_relationship_is_denied_and_denial_is_audited()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var stranger = await _f.SeedUserAsync(UserRole.AttendingVet);
        var r = await _f.RunAs<IClinicalService, Result<ConsultationDto>>(stranger, Vet, svc =>
            svc.RegisterConsultationAsync(new RegisterConsultationCommand(s.RecordId, stranger, _f.Clock.Now, "x", null)));
        r.Success.Should().BeFalse();
        r.ErrorType.Should().Be(ErrorType.Forbidden);

        var read = await _f.RunAs<IClinicalService, Result<ClinicalRecordDto>>(stranger, Vet, svc => svc.GetHistoryAsync(s.PetId));
        read.ErrorType.Should().Be(ErrorType.Forbidden);

        using var scope = _f.NewScope();
        (await _f.Get<AppDbContext>(scope).AuditLogs.CountAsync(a => a.ActorUserId == stranger && a.Result == AuditLog.Results.Denied)).Should().Be(2);
    }

    [Fact]
    public async Task Revoked_regente_loses_write_access_and_only_one_active_regente_exists()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var vet2 = await _f.SeedUserAsync(UserRole.AttendingVet);
        await _f.RunAs<IClinicService, Result<ClinicMemberDto>>(s.AdminId, new[] { UserRole.PlatformAdmin }, svc => svc.AddMemberAsync(new AddMemberCommand(s.ClinicId, vet2)));
        var assign = await _f.RunAs<IVetRelationshipService, Result<AttendingVetRelationshipDto>>(s.OwnerId, new[] { UserRole.PetOwner },
            svc => svc.AssignAsync(new AssignAttendingVetCommand(s.PetId, vet2, s.ClinicId)));
        assign.Success.Should().BeTrue();

        using (var scope = _f.NewScope())
            (await _f.Get<AppDbContext>(scope).AttendingVetRelationships.CountAsync(r => r.PetId == s.PetId && r.IsActive)).Should().Be(1);

        var old = await _f.RunAs<IClinicalService, Result<ConsultationDto>>(s.VetId, Vet, svc =>
            svc.RegisterConsultationAsync(new RegisterConsultationCommand(s.RecordId, s.VetId, _f.Clock.Now, "x", null)));
        old.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Prescription_requires_authorized_vet_and_supports_addenda()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var c = await _f.RunAs<IClinicalService, Result<ConsultationDto>>(s.VetId, Vet, svc =>
            svc.RegisterConsultationAsync(new RegisterConsultationCommand(s.RecordId, s.VetId, _f.Clock.Now, "Otitis", null)));
        var p = await _f.RunAs<IClinicalService, Result<PrescriptionDto>>(s.VetId, Vet, svc =>
            svc.RegisterPrescriptionAsync(new RegisterPrescriptionCommand(c.Value!.Id, "Amoxicilina", "250mg", "Oral", "Cada 12h", 7)));
        p.Success.Should().BeTrue();
        var add = await _f.RunAs<IClinicalService, Result<AddendumDto>>(s.VetId, Vet, svc =>
            svc.AddPrescriptionAddendumAsync(new AddPrescriptionAddendumCommand(p.Value!.Id, "Ajuste", "Extender a 10 días")));
        add.Success.Should().BeTrue();

        var stranger = await _f.SeedUserAsync(UserRole.AttendingVet);
        var denied = await _f.RunAs<IClinicalService, Result<PrescriptionDto>>(stranger, Vet, svc =>
            svc.RegisterPrescriptionAsync(new RegisterPrescriptionCommand(c.Value!.Id, "X", "1", "Oral", "1", 1)));
        denied.ErrorType.Should().Be(ErrorType.Forbidden);
    }
}
