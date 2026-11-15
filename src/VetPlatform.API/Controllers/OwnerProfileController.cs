using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.API.ViewModels;
using VetPlatform.Application.Features.Owners;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.API.Controllers;

/// <summary>
/// CRUD de los datos básicos del propietario (PBI-01): registrar (Create), consultar (Index),
/// editar (Edit) y eliminar (Delete). Siempre trabaja sobre el perfil del usuario con sesión iniciada.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.RequirePetOwner)]
public class OwnerProfileController : MvcControllerBase
{
    private readonly IOwnerService _owners;
    private readonly ILogger<OwnerProfileController> _logger;

    public OwnerProfileController(IOwnerService owners, ILogger<OwnerProfileController> logger)
    {
        _owners = owners;
        _logger = logger;
    }

    // READ ---------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _owners.GetMyProfileAsync(ct);
        if (result.ErrorType == VetPlatform.Application.Common.ErrorType.NotFound)
        {
            return RedirectToAction(nameof(Create));
        }

        if (!result.Success)
        {
            AddErrors(result);
            return View((OwnerProfileDto?)null);
        }

        return View(result.Value);
    }

    // CREATE -------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var existing = await _owners.GetMyProfileAsync(ct);
        if (existing.Success)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(new OwnerProfileViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OwnerProfileViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _owners.CreateMyProfileAsync(
            new CreateOwnerProfileCommand(model.IdentificationNumber, model.Phone, model.Address), ct);

        if (!result.Success)
        {
            AddErrors(result);
            return View(model);
        }

        _logger.LogInformation("Perfil de propietario {ProfileId} registrado", result.Value?.Id);
        TempData["SuccessMessage"] = "Tus datos de propietario se registraron correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // UPDATE -------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Edit(CancellationToken ct)
    {
        var result = await _owners.GetMyProfileAsync(ct);
        if (!result.Success || result.Value is null)
        {
            return RedirectToAction(nameof(Create));
        }

        var p = result.Value;
        return View(new OwnerProfileViewModel
        {
            IdentificationNumber = p.IdentificationNumber,
            Phone = p.Phone,
            Address = p.Address
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OwnerProfileViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _owners.UpdateMyProfileAsync(
            new UpdateOwnerProfileCommand(model.IdentificationNumber, model.Phone, model.Address), ct);

        if (!result.Success)
        {
            AddErrors(result);
            return View(model);
        }

        TempData["SuccessMessage"] = "Tus datos se actualizaron correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // DELETE -------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        var result = await _owners.GetMyProfileAsync(ct);
        if (!result.Success)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(CancellationToken ct)
    {
        var result = await _owners.DeleteMyProfileAsync(ct);
        if (!result.Success)
        {
            AddErrors(result);
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Tus datos de propietario fueron eliminados.";
        return RedirectToAction(nameof(Create));
    }
}
