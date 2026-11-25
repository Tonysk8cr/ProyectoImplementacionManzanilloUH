using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.API.ViewModels;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Application.Features.Organizations;
using VetPlatform.Application.Features.Scheduling;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.API.Controllers;

[Authorize]
public sealed class AppointmentsController : MvcControllerBase
{
    private readonly IClinicalService _clinical;
    private readonly IClinicService _clinics;
    private readonly ISchedulingService _scheduling;
    private readonly TimeProvider _time;

    public AppointmentsController(IClinicalService clinical, IClinicService clinics, ISchedulingService scheduling, TimeProvider time)
    {
        _clinical = clinical;
        _clinics = clinics;
        _scheduling = scheduling;
        _time = time;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? clinicId, CancellationToken ct)
    {
        var model = new AppointmentsIndexViewModel
        {
            SelectedClinicId = clinicId,
            IsClinicManager = User.IsInRole(nameof(UserRole.ClinicAdmin)) || User.IsInRole(nameof(UserRole.PlatformAdmin))
        };

        if (model.IsClinicManager)
        {
            var clinics = await _clinics.GetManagedClinicsAsync(ct);
            if (!clinics.Success)
                AddErrors(clinics);
            else
                model.Clinics = clinics.Value ?? Array.Empty<ClinicDto>();
        }

        var appointments = await _scheduling.GetAppointmentsForCurrentUserAsync(clinicId, ct);
        if (!appointments.Success)
            AddErrors(appointments);
        else
            model.Appointments = appointments.Value ?? Array.Empty<AppointmentDto>();

        return View(model);
    }

    [Authorize(Policy = AuthorizationPolicies.RequirePetOwner)]
    [HttpGet]
    public async Task<IActionResult> Book(Guid? clinicId, Guid? petId, DateOnly? date, CancellationToken ct)
    {
        var model = new AppointmentBookingViewModel
        {
            ClinicId = clinicId,
            PetId = petId,
            Date = date ?? DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime)
        };
        await PopulateBookingOptionsAsync(model, ct);
        return View(model);
    }

    [Authorize(Policy = AuthorizationPolicies.RequirePetOwner)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(AppointmentBookingViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulateBookingOptionsAsync(model, ct);
            return View(model);
        }

        var result = await _scheduling.BookAppointmentAsync(
            new BookAppointmentCommand(model.ClinicId!.Value, model.PetId!.Value, model.SlotId!.Value, model.Reason!), ct);
        if (!result.Success || result.Value is null)
        {
            AddErrors(result);
            model.SlotId = null;
            ModelState.Remove(nameof(model.SlotId));
            await PopulateBookingOptionsAsync(model, ct);
            return View(model);
        }

        return RedirectToAction(nameof(Confirmation), new { id = result.Value.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(Guid id, CancellationToken ct)
    {
        var result = await _scheduling.GetAppointmentAsync(id, ct);
        if (!result.Success || result.Value is null)
        {
            AddErrors(result);
            return View("Index", new AppointmentsIndexViewModel());
        }
        return View(result.Value);
    }

    private async Task PopulateBookingOptionsAsync(AppointmentBookingViewModel model, CancellationToken ct)
    {
        var pets = await _clinical.GetMyPetsAsync(ct);
        if (!pets.Success)
            AddErrors(pets);
        else
            model.Pets = pets.Value ?? Array.Empty<PetDto>();

        var clinics = await _clinics.GetActiveClinicsAsync(ct);
        if (!clinics.Success)
            AddErrors(clinics);
        else
            model.Clinics = clinics.Value ?? Array.Empty<ClinicDto>();

        if (model.ClinicId.HasValue && model.Date.HasValue)
        {
            var slots = await _scheduling.GetAvailabilityAsync(model.ClinicId, null, model.Date, ct);
            if (!slots.Success)
                AddErrors(slots);
            else
                model.Slots = slots.Value ?? Array.Empty<AvailabilitySlotDto>();
        }
    }
}
