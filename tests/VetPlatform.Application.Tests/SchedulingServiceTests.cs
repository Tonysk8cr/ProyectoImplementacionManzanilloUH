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
        var appt = await _f.RunAs<ISchedulingService, Result<AppointmentDto>>(s.OwnerId, Owner, svc => svc.BookAppointmentAsync(new BookAppointmentCommand(s.PetId, slot.Value!.Id)));
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
        (await _f.RunAs<ISchedulingService, Result<AppointmentDto>>(s.OwnerId, Owner, svc => svc.BookAppointmentAsync(new BookAppointmentCommand(s.PetId, slot.Value!.Id)))).Success.Should().BeTrue();
        var second = await _f.RunAs<ISchedulingService, Result<AppointmentDto>>(owner2, Owner, svc => svc.BookAppointmentAsync(new BookAppointmentCommand(pet2.Value!.Id, slot.Value!.Id)));
        second.ErrorType.Should().Be(ErrorType.Conflict);
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
        var r = await _f.RunAs<ISchedulingService, Result<AppointmentDto>>(other, Owner, svc => svc.BookAppointmentAsync(new BookAppointmentCommand(s.PetId, slot.Value!.Id)));
        r.ErrorType.Should().Be(ErrorType.Forbidden);
    }
}
