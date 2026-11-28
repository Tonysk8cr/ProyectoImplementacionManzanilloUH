using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.API.ViewModels;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Scheduling;

namespace VetPlatform.API.Controllers;

[Authorize(Roles = "PlatformAdmin,ClinicAdmin,AttendingVet,EmergencyVet,ExternalVet")]
public class ClinicAppointmentsController(IClinicAppointmentService appointments, TimeProvider time) : MvcControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(Guid? clinicId, DateOnly? date, string? status, bool allDates = false, CancellationToken ct = default)
    {
        var clinics = await appointments.GetClinicsAsync(ct);
        if (!clinics.Success) return Forbid();
        var model = new ClinicAppointmentsViewModel
        {
            Clinics = clinics.Value!, ClinicId = clinicId ?? clinics.Value!.FirstOrDefault()?.Id,
            Date = date ?? (allDates ? null : DateOnly.FromDateTime(time.GetUtcNow().ToOffset(TimeSpan.FromHours(-6)).DateTime)), Status = status
        };
        if (model.ClinicId.HasValue)
        {
            var result = await appointments.SearchAsync(model.ClinicId.Value, model.Date, status, ct);
            if (result.ErrorType == ErrorType.Forbidden) return Forbid();
            if (!result.Success) AddErrors(result);
            else model.Appointments = result.Value!;
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var result = await appointments.GetAsync(id, ct);
        if (result.ErrorType == ErrorType.Forbidden) return Forbid();
        if (!result.Success) return NotFound();
        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(Guid id, string status, CancellationToken ct)
    {
        var result = await appointments.ChangeStatusAsync(id, status, ct);
        if (result.ErrorType == ErrorType.Forbidden) return Forbid();
        if (result.ErrorType == ErrorType.NotFound) return NotFound();
        if (!result.Success)
        {
            AddErrors(result);
            var detail = await appointments.GetAsync(id, ct);
            if (!detail.Success) return NotFound();
            Response.StatusCode = result.ErrorType == ErrorType.Conflict ? 409 : 400;
            return View("Details", detail.Value);
        }
        TempData["SuccessMessage"] = "Estado de la cita actualizado correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
