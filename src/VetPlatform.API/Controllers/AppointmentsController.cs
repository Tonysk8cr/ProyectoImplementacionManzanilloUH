using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetPlatform.API.ViewModels;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Application.Features.Scheduling;
using VetPlatform.Infrastructure.Data;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.API.Controllers;

[Authorize(Policy = AuthorizationPolicies.RequirePetOwner)]
public class AppointmentsController(IClinicalService clinical, ISchedulingService scheduling, AppDbContext db, TimeProvider time) : MvcControllerBase
{
    // All dates in the booking screen use Costa Rica's UTC-6, independently of server timezone.
    private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(-6);

    [HttpGet]
    public async Task<IActionResult> Index(Guid? petId, DateOnly? date, string? reason, CancellationToken ct)
    {
        var model = new AppointmentViewModel { PetId = petId, Date = date ?? DateOnly.FromDateTime(time.GetUtcNow().ToOffset(LocalOffset).DateTime), Reason = reason ?? "Consulta" };
        await PopulateAsync(model, ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm([Bind("PetId,Date,SlotId,Reason")] AppointmentViewModel model, CancellationToken ct)
    {
        await PopulateAsync(model, ct);
        if (model.Date < model.Today) ModelState.AddModelError(nameof(model.Date), "Selecciona una fecha actual o futura.");
        if (model.SlotId.HasValue && !model.Slots.Any(s => s.Id == model.SlotId))
            ModelState.AddModelError(nameof(model.SlotId), "El horario ya no está disponible. Selecciona otro.");
        if (!ModelState.IsValid) return View("Index", model);
        var result = await scheduling.BookAppointmentAsync(new(model.PetId!.Value, model.SlotId!.Value, model.Reason), ct);
        if (!result.Success)
        {
            AddErrors(result);
            await PopulateAsync(model, ct);
            return View("Index", model);
        }
        return RedirectToAction(nameof(Confirmation), new { id = result.Value!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(Guid id, CancellationToken ct)
    {
        var summary = await (from a in db.Appointments.AsNoTracking()
                             join pet in db.Pets on a.PetId equals pet.Id
                             join slot in db.AvailabilitySlots on a.SlotId equals slot.Id
                             join clinic in db.Clinics on a.ClinicId equals clinic.Id
                             where a.Id == id && a.OwnerId == CurrentUserId
                             select new AppointmentConfirmationViewModel(pet.Name, a.Reason, slot.StartTime, clinic.Name)).FirstOrDefaultAsync(ct);
        if (summary == null) return NotFound();
        return View(summary with { StartTime = summary.StartTime.ToOffset(LocalOffset) });
    }

    private async Task PopulateAsync(AppointmentViewModel model, CancellationToken ct)
    {
        model.Today = DateOnly.FromDateTime(time.GetUtcNow().ToOffset(LocalOffset).DateTime);
        var pets = await clinical.GetMyPetsAsync(ct);
        if (!pets.Success) AddErrors(pets);
        model.Pets = pets.Value ?? Array.Empty<VetPlatform.Application.Features.Clinical.PetDto>();
        model.Slots = Array.Empty<AppointmentSlotOption>();
        if (!model.PetId.HasValue) return;
        if (!model.Pets.Any(p => p.Id == model.PetId))
        {
            ModelState.AddModelError(nameof(model.PetId), "Selecciona una de tus mascotas.");
            return;
        }
        if (!model.Date.HasValue) return;
        // Filter after converting to the displayed timezone: a late local slot may be on the next UTC day.
        var available = await scheduling.GetAvailabilityAsync(null, null, null, ct);
        if (!available.Success) { AddErrors(available); return; }
        var slots = available.Value!.Where(s => DateOnly.FromDateTime(s.StartTime.ToOffset(LocalOffset).DateTime) == model.Date).ToList();
        var clinicIds = slots.Select(s => s.ClinicId).Distinct().ToArray();
        var vetIds = slots.Select(s => s.VetUserId).Distinct().ToArray();
        var clinics = await db.Clinics.Where(c => clinicIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        var vets = await db.Users.Where(u => vetIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        model.Slots = slots.Select(s => new AppointmentSlotOption(s.Id, s.StartTime.ToOffset(LocalOffset), clinics.GetValueOrDefault(s.ClinicId, "Clínica"), vets.GetValueOrDefault(s.VetUserId, "Veterinario"))).OrderBy(s => s.StartTime).ToList();
    }
}
