using FluentAssertions;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Scheduling;
using VetPlatform.Domain.Enums;
using Xunit;

namespace VetPlatform.Application.Tests;

public class SchedulingServiceTests : IDisposable
{
    private readonly TestFixture _f = new();
    public void Dispose() => _f.Dispose();
    private static readonly UserRole[] Admin = { UserRole.PlatformAdmin };
    private static readonly UserRole[] Owner = { UserRole.PetOwner };

    private Task<Result<AvailabilitySlotDto>> CreateSlot(ClinicalScenario s, int hoursFromNow) =>
        _f.RunAs<ISchedulingService, Result<AvailabilitySlotDto>>(s.AdminId, Admin, svc => svc.CreateSlotAsync(
            new CreateAvailabilitySlotCommand(s.ClinicId, s.VetId, _f.Clock.Now.AddHours(hoursFromNow), _f.Clock.Now.AddHours(hoursFromNow).AddMinutes(30))));

    [Fact]
    public async Task Owner_books_available_slot()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var slot = await CreateSlot(s, 24);
        slot.Success.Should().BeTrue(string.Join(",", slot.Errors));
        var appt = await _f.RunAs<ISchedulingService, Result<AppointmentDto>>(s.OwnerId, Owner, svc => svc.BookAppointmentAsync(
            new BookAppointmentCommand(s.ClinicId, s.PetId, slot.Value!.Id, "Consulta general")));
        appt.Success.Should().BeTrue(string.Join(",", appt.Errors));
        appt.Value!.Status.Should().Be("Booked");

        var free = await _f.RunAs<ISchedulingService, Result<IReadOnlyList<AvailabilitySlotDto>>>(s.OwnerId, Owner, svc => svc.GetAvailabilityAsync(s.ClinicId, null, null));
        free.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Second_booking_of_same_slot_is_rejected_at_confirmation()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var slot = await CreateSlot(s, 24);
        var owner2 = await _f.SeedUserAsync(UserRole.PetOwner);
        var pet2 = await _f.RunAs<VetPlatform.Application.Features.Clinical.IClinicalService, Result<VetPlatform.Application.Features.Clinical.PetDto>>(owner2, Owner,
            svc => svc.CreatePetAsync(new VetPlatform.Application.Features.Clinical.CreatePetCommand(owner2, "Michi", "Gato", null, null)));

        // Both owners saw the slot as free; only the first confirmation wins.
        (await _f.RunAs<ISchedulingService, Result<AppointmentDto>>(s.OwnerId, Owner, svc => svc.BookAppointmentAsync(
            new BookAppointmentCommand(s.ClinicId, s.PetId, slot.Value!.Id, "Consulta general")))).Success.Should().BeTrue();
        var second = await _f.RunAs<ISchedulingService, Result<AppointmentDto>>(owner2, Owner, svc => svc.BookAppointmentAsync(
            new BookAppointmentCommand(s.ClinicId, pet2.Value!.Id, slot.Value!.Id, "Consulta general")));
        second.ErrorType.Should().Be(ErrorType.Conflict);
        var ownerAppointments = await _f.RunAs<ISchedulingService, Result<IReadOnlyList<AppointmentDto>>>(
            s.OwnerId, Owner, svc => svc.GetAppointmentsForCurrentUserAsync());
        ownerAppointments.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task Overlapping_slots_for_same_vet_are_rejected()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        (await CreateSlot(s, 24)).Success.Should().BeTrue();
        (await CreateSlot(s, 24)).ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Cannot_book_for_someone_elses_pet()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var slot = await CreateSlot(s, 24);
        var other = await _f.SeedUserAsync(UserRole.PetOwner);
        var r = await _f.RunAs<ISchedulingService, Result<AppointmentDto>>(other, Owner, svc => svc.BookAppointmentAsync(
            new BookAppointmentCommand(s.ClinicId, s.PetId, slot.Value!.Id, "Consulta general")));
        r.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Appointment_summary_is_visible_to_owner_and_clinic_vet()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var slot = await CreateSlot(s, 24);
        var booked = await _f.RunAs<ISchedulingService, Result<AppointmentDto>>(s.OwnerId, Owner, svc =>
            svc.BookAppointmentAsync(new BookAppointmentCommand(s.ClinicId, s.PetId, slot.Value!.Id, "Vacunación")));

        booked.Success.Should().BeTrue(string.Join(",", booked.Errors));
        booked.Value!.ClinicName.Should().Be("Clínica Central");
        booked.Value.PetName.Should().Be("Firulais");
        booked.Value.Reason.Should().Be("Vacunación");
        booked.Value.StartTime.Should().Be(slot.Value!.StartTime);
        booked.Value.Status.Should().Be("Booked");

        var ownerAppointments = await _f.RunAs<ISchedulingService, Result<IReadOnlyList<AppointmentDto>>>(
            s.OwnerId, Owner, svc => svc.GetAppointmentsForCurrentUserAsync());
        var vetAppointments = await _f.RunAs<ISchedulingService, Result<IReadOnlyList<AppointmentDto>>>(
            s.VetId, new[] { UserRole.AttendingVet }, svc => svc.GetAppointmentsForCurrentUserAsync(s.ClinicId));

        ownerAppointments.Value.Should().ContainSingle().Which.Id.Should().Be(booked.Value.Id);
        vetAppointments.Value.Should().ContainSingle().Which.Should().BeEquivalentTo(ownerAppointments.Value!.Single());
    }

    [Fact]
    public async Task Missing_booking_details_are_reported_without_registering_an_appointment()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var result = await _f.RunAs<ISchedulingService, Result<AppointmentDto>>(s.OwnerId, Owner, svc =>
            svc.BookAppointmentAsync(new BookAppointmentCommand(Guid.Empty, Guid.Empty, Guid.Empty, " ")));

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("veterinaria"));
        result.Errors.Should().Contain(e => e.Contains("mascota"));
        result.Errors.Should().Contain(e => e.Contains("horario"));
        result.Errors.Should().Contain(e => e.Contains("motivo"));

        var appointments = await _f.RunAs<ISchedulingService, Result<IReadOnlyList<AppointmentDto>>>(
            s.OwnerId, Owner, svc => svc.GetAppointmentsForCurrentUserAsync());
        appointments.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Booking_rejects_a_clinic_that_does_not_own_the_selected_slot()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var slot = await CreateSlot(s, 24);
        var result = await _f.RunAs<ISchedulingService, Result<AppointmentDto>>(s.OwnerId, Owner, svc =>
            svc.BookAppointmentAsync(new BookAppointmentCommand(Guid.NewGuid(), s.PetId, slot.Value!.Id, "Consulta")));

        result.ErrorType.Should().Be(ErrorType.Conflict);
        var appointments = await _f.RunAs<ISchedulingService, Result<IReadOnlyList<AppointmentDto>>>(
            s.OwnerId, Owner, svc => svc.GetAppointmentsForCurrentUserAsync());
        appointments.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Availability_hides_slots_after_the_vet_leaves_the_clinic()
    {
        var s = await ClinicalScenario.CreateAsync(_f);
        var slot = await CreateSlot(s, 24);
        slot.Success.Should().BeTrue();

        var removed = await _f.RunAs<VetPlatform.Application.Features.Organizations.IClinicService, Result<bool>>(
            s.AdminId, Admin, svc => svc.RemoveMemberAsync(
                new VetPlatform.Application.Features.Organizations.RemoveMemberCommand(s.ClinicId, s.VetId)));
        removed.Success.Should().BeTrue(string.Join(",", removed.Errors));

        var availability = await _f.RunAs<ISchedulingService, Result<IReadOnlyList<AvailabilitySlotDto>>>(
            s.OwnerId, Owner, svc => svc.GetAvailabilityAsync(s.ClinicId, null, null));
        availability.Value.Should().BeEmpty();

        var staleBooking = await _f.RunAs<ISchedulingService, Result<AppointmentDto>>(s.OwnerId, Owner, svc =>
            svc.BookAppointmentAsync(new BookAppointmentCommand(s.ClinicId, s.PetId, slot.Value!.Id, "Consulta")));
        staleBooking.ErrorType.Should().Be(ErrorType.Conflict);
        staleBooking.Errors.Should().Contain(e => e.Contains("seleccione otro horario"));
    }
}
