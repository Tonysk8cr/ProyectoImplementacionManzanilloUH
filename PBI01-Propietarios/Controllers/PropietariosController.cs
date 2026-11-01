using Microsoft.AspNetCore.Mvc;
using PBI01.Propietarios.Models;
using PBI01.Propietarios.Services;

namespace PBI01.Propietarios.Controllers;

/// <summary>CRUD visual de propietarios (PBI-01): listar, ver, crear, editar y eliminar.</summary>
public class PropietariosController : Controller
{
    private readonly IPropietarioRepository _repo;

    public PropietariosController(IPropietarioRepository repo)
    {
        _repo = repo;
    }

    // READ (lista) -------------------------------------------------------
    [HttpGet]
    public IActionResult Index() => View(_repo.ObtenerTodos());

    // READ (detalle) -----------------------------------------------------
    [HttpGet]
    public IActionResult Details(Guid id)
    {
        var propietario = _repo.ObtenerPorId(id);
        return propietario is null ? NotFound() : View(propietario);
    }

    // CREATE -------------------------------------------------------------
    [HttpGet]
    public IActionResult Create() => View(new Propietario());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Propietario model)
    {
        Normalizar(model);
        ValidarIdentificacionUnica(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _repo.Agregar(model);
        TempData["SuccessMessage"] = "El propietario se registró correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // UPDATE -------------------------------------------------------------
    [HttpGet]
    public IActionResult Edit(Guid id)
    {
        var propietario = _repo.ObtenerPorId(id);
        return propietario is null ? NotFound() : View(propietario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, Propietario model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        Normalizar(model);
        ValidarIdentificacionUnica(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!_repo.Actualizar(model))
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Los datos se actualizaron correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // DELETE -------------------------------------------------------------
    [HttpGet]
    public IActionResult Delete(Guid id)
    {
        var propietario = _repo.ObtenerPorId(id);
        return propietario is null ? NotFound() : View(propietario);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(Guid id)
    {
        if (_repo.Eliminar(id))
        {
            TempData["SuccessMessage"] = "El propietario fue eliminado.";
        }

        return RedirectToAction(nameof(Index));
    }

    // Ayudas -------------------------------------------------------------
    private static void Normalizar(Propietario model)
    {
        model.NombreCompleto = (model.NombreCompleto ?? string.Empty).Trim();
        model.Identificacion = (model.Identificacion ?? string.Empty).Trim().ToUpperInvariant();
        model.Correo = (model.Correo ?? string.Empty).Trim().ToLowerInvariant();
        model.Telefono = (model.Telefono ?? string.Empty).Trim();
        model.Direccion = string.IsNullOrWhiteSpace(model.Direccion) ? null : model.Direccion.Trim();
    }

    private void ValidarIdentificacionUnica(Propietario model)
    {
        if (!string.IsNullOrEmpty(model.Identificacion)
            && _repo.ExisteIdentificacion(model.Identificacion, model.Id))
        {
            ModelState.AddModelError(nameof(Propietario.Identificacion),
                "Ya existe un propietario registrado con esa identificación.");
        }
    }
}
