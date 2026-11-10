using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.API.ViewModels;
using VetPlatform.Application.Features.Clinical;

namespace VetPlatform.API.Controllers;

/// <summary>
/// Flujo Sprint 1 del propietario: lista de mascotas (PBI-01), registro de mascota (PBI-02/PBI-03)
/// y detalle con historial clínico (PBI-04).
/// </summary>
[Authorize]
public class PetsController : MvcControllerBase
{
    private readonly IClinicalService _clinical;
    private readonly ILogger<PetsController> _logger;

    public PetsController(IClinicalService clinical, ILogger<PetsController> logger)
    {
        _clinical = clinical;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _clinical.GetMyPetsAsync(ct);
        if (!result.Success)
        {
            AddErrors(result);
            return View(Array.Empty<PetDto>());
        }

        return View(result.Value);
    }

    [HttpGet]
    public IActionResult Create() => View(new PetViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PetViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ownerId = CurrentUserId;
        if (ownerId is null)
        {
            return RedirectToAction(nameof(AccountController.Login), "Account");
        }

        var result = await _clinical.CreatePetAsync(
            new CreatePetCommand(ownerId.Value, model.Name, model.Species, model.Breed, model.BirthDate), ct);

        if (!result.Success)
        {
            AddErrors(result);
            return View(model);
        }

        var pet = result.Value;
        if (pet is null)
        {
            ModelState.AddModelError(string.Empty, "No se pudo registrar la mascota. Inténtalo de nuevo.");
            return View(model);
        }

        _logger.LogInformation("Mascota {PetId} registrada por el usuario {UserId}",
            pet.Id, ownerId);

        TempData["SuccessMessage"] = $"Mascota \"{pet.Name}\" registrada correctamente.";
        return RedirectToAction(nameof(Details), new { id = pet.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var petResult = await _clinical.GetPetAsync(id, ct);
        var pet = petResult.Value;
        if (!petResult.Success || pet is null)
        {
            AddErrors(petResult);
            return View("Index", Array.Empty<PetDto>());
        }

        var historyResult = await _clinical.GetHistoryAsync(id, ct);

        var viewModel = new PetDetailsViewModel
        {
            Pet = pet,
            History = historyResult.Value
        };

        if (!historyResult.Success)
        {
            AddErrors(historyResult);
        }

        return View(viewModel);
    }
}
