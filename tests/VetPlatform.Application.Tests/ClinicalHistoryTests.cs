using FluentAssertions;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Application.Features.Organizations;
using VetPlatform.Domain.Enums;
using Xunit;

namespace VetPlatform.Application.Tests;

/// <summary>Historial de consultas clínicas del paciente (vista del médico veterinario).</summary>
public class ClinicalHistoryTests : IDisposable
{
    private readonly TestFixture _f = new();
    public void Dispose() => _f.Dispose();
    private static readonly UserRole[] Vet = { UserRole.AttendingVet };
    private static readonly UserRole[] Owner = { UserRole.PetOwner };

    private Task<Result<ConsultationDto>> RegisterAsync(ClinicalScenario s, Guid recordId, DateTimeOffset date, string notes) =>
        _f.RunAs<IClinicalService, Result<ConsultationDto>>(s.VetId, Vet, svc =>
            svc.RegisterConsultationAsync(new RegisterConsultationCommand(recordId, s.VetId, date, notes, null)));

    /// <summary>Segunda mascota del mismo tutor, atendida por el mismo veterinario.</summary>
    private async Task<(Guid PetId, Guid RecordId)> SecondPatientAsync(ClinicalScenario s)
    {
        var pet = await _f.RunAs<IClinicalService, Result<PetDto>>(s.OwnerId, Owner,
            svc => svc.CreatePetAsync(new CreatePetCommand(s.OwnerId, "Michi", "Gato", null, null)));
        var hist = await _f.RunAs<IClinicalService, Result<ClinicalRecordDto>>(s.OwnerId, Owner, svc => svc.GetHistoryAsync(pet.Value!.Id));
        await _f.RunAs<IVetRelationshipService, Result<AttendingVetRelationshipDto>>(s.OwnerId, Owner,
            svc => svc.AssignAsync(new AssignAttendingVetCommand(pet.Value!.Id, s.VetId, s.ClinicId)));
        return (pet.Value!.Id, hist.Value!.Id);
    }

    [Fact]
    public async Task History_lists_consultations_most_recent_first()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var now = _f.Clock.Now;
        await RegisterAsync(s, s.RecordId, now.AddDays(-10), "Antigua");
        await RegisterAsync(s, s.RecordId, now.AddDays(-1), "Reciente");
        await RegisterAsync(s, s.RecordId, now.AddDays(-5), "Intermedia");

        var r = await _f.RunAs<IClinicalService, Result<ClinicalRecordDto>>(s.VetId, Vet, svc => svc.GetHistoryAsync(s.PetId));

        r.Success.Should().BeTrue();
        r.Value!.Consultations.Select(c => c.Notes).Should().Equal("Reciente", "Intermedia", "Antigua");
        r.Value.Consultations.Should().OnlyContain(c => c.AttendanceDate != default);
    }

    [Fact]
    public async Task History_with_equal_dates_is_ordered_by_registration_most_recent_first()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var sameDate = _f.Clock.Now.AddHours(-2);
        await RegisterAsync(s, s.RecordId, sameDate, "Primera registrada");
        _f.Clock.Advance(TimeSpan.FromMinutes(5));
        await RegisterAsync(s, s.RecordId, sameDate, "Segunda registrada");

        for (var i = 0; i < 3; i++) // el orden debe ser estable entre lecturas
        {
            var r = await _f.RunAs<IClinicalService, Result<ClinicalRecordDto>>(s.VetId, Vet, svc => svc.GetHistoryAsync(s.PetId));
            r.Value!.Consultations.Select(c => c.Notes).Should().Equal("Segunda registrada", "Primera registrada");
        }
    }

    [Fact]
    public async Task History_only_contains_consultations_of_the_requested_patient()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var (otherPetId, otherRecordId) = await SecondPatientAsync(s);
        await RegisterAsync(s, s.RecordId, _f.Clock.Now, "Consulta de Firulais");
        await RegisterAsync(s, otherRecordId, _f.Clock.Now, "Consulta de Michi");

        var a = await _f.RunAs<IClinicalService, Result<ClinicalRecordDto>>(s.VetId, Vet, svc => svc.GetHistoryAsync(s.PetId));
        var b = await _f.RunAs<IClinicalService, Result<ClinicalRecordDto>>(s.VetId, Vet, svc => svc.GetHistoryAsync(otherPetId));

        a.Value!.Consultations.Should().ContainSingle().Which.Notes.Should().Be("Consulta de Firulais");
        a.Value.Consultations.Should().OnlyContain(c => c.ClinicalRecordId == s.RecordId);
        b.Value!.Consultations.Should().ContainSingle().Which.Notes.Should().Be("Consulta de Michi");
    }

    [Fact]
    public async Task History_of_patient_without_consultations_is_empty()
    {
        var s = await ClinicalScenario.CreateAsync(_f);

        var r = await _f.RunAs<IClinicalService, Result<ClinicalRecordDto>>(s.VetId, Vet, svc => svc.GetHistoryAsync(s.PetId));

        r.Success.Should().BeTrue();
        r.Value!.Consultations.Should().BeEmpty();
    }

    [Fact]
    public async Task Patient_consultation_detail_includes_prescriptions_and_addenda()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var c = await RegisterAsync(s, s.RecordId, _f.Clock.Now, "Otitis leve");
        await _f.RunAs<IClinicalService, Result<PrescriptionDto>>(s.VetId, Vet, svc =>
            svc.RegisterPrescriptionAsync(new RegisterPrescriptionCommand(c.Value!.Id, "Otomax", "5 gotas", "Ótica", "12h", 7)));
        await _f.RunAs<IClinicalService, Result<AddendumDto>>(s.VetId, Vet, svc =>
            svc.AddAddendumAsync(new AddAddendumCommand(c.Value!.Id, "Aclaración", "Oído derecho")));

        var r = await _f.RunAs<IClinicalService, Result<ConsultationDto>>(s.VetId, Vet, svc => svc.GetPetConsultationAsync(s.PetId, c.Value!.Id));

        r.Success.Should().BeTrue(string.Join(",", r.Errors));
        r.Value!.Notes.Should().Be("Otitis leve");
        r.Value.Prescriptions.Should().ContainSingle(p => p.Medication == "Otomax");
        r.Value.Addenda.Should().ContainSingle(a => a.Content == "Oído derecho");
    }

    [Fact]
    public async Task Patient_consultation_detail_of_unknown_consultation_is_not_found()
    {
        var s = await ClinicalScenario.CreateAsync(_f);

        var r = await _f.RunAs<IClinicalService, Result<ConsultationDto>>(s.VetId, Vet, svc => svc.GetPetConsultationAsync(s.PetId, Guid.NewGuid()));

        r.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Consultation_of_another_patient_is_not_returned_under_this_patient()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var (_, otherRecordId) = await SecondPatientAsync(s);
        var other = await RegisterAsync(s, otherRecordId, _f.Clock.Now, "Consulta de Michi");

        var r = await _f.RunAs<IClinicalService, Result<ConsultationDto>>(s.VetId, Vet, svc => svc.GetPetConsultationAsync(s.PetId, other.Value!.Id));

        r.Success.Should().BeFalse();
        r.ErrorType.Should().Be(ErrorType.NotFound);
        r.Value.Should().BeNull();
    }

    [Fact]
    public async Task Vet_without_relationship_cannot_read_patient_consultation_detail()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var c = await RegisterAsync(s, s.RecordId, _f.Clock.Now, "Confidencial");
        var stranger = await _f.SeedUserAsync(UserRole.AttendingVet);

        var r = await _f.RunAs<IClinicalService, Result<ConsultationDto>>(stranger, Vet, svc => svc.GetPetConsultationAsync(s.PetId, c.Value!.Id));

        r.ErrorType.Should().Be(ErrorType.Forbidden);
        r.Value.Should().BeNull();
    }

    [Fact]
    public async Task My_patients_lists_only_pets_with_active_relationship_to_the_vet()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        await ClinicalScenario.CreateAsync(_f); // otra mascota con otro veterinario

        var r = await _f.RunAs<IClinicalService, Result<IReadOnlyList<PetDto>>>(s.VetId, Vet, svc => svc.GetMyPatientsAsync());

        r.Success.Should().BeTrue();
        r.Value!.Should().ContainSingle().Which.Id.Should().Be(s.PetId);
    }

    [Fact]
    public async Task My_patients_excludes_revoked_relationships()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        await _f.RunAs<IVetRelationshipService, Result<bool>>(s.OwnerId, Owner, svc => svc.RevokeAsync(new RevokeAttendingVetCommand(s.PetId)));

        var r = await _f.RunAs<IClinicalService, Result<IReadOnlyList<PetDto>>>(s.VetId, Vet, svc => svc.GetMyPatientsAsync());

        r.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task My_patients_is_forbidden_for_non_vets()
    {
        var s = await ClinicalScenario.CreateAsync(_f);

        var r = await _f.RunAs<IClinicalService, Result<IReadOnlyList<PetDto>>>(s.OwnerId, Owner, svc => svc.GetMyPatientsAsync());

        r.ErrorType.Should().Be(ErrorType.Forbidden);
    }
}
