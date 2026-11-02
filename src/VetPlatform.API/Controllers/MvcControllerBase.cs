using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Common;

namespace VetPlatform.API.Controllers;

/// <summary>
/// Base de los controladores MVC. Traduce el <see cref="Result{T}"/> de la capa Application
/// a errores de <c>ModelState</c>, que las vistas muestran junto a cada campo.
/// </summary>
public abstract class MvcControllerBase : Controller
{
    /// <summary>Identificador del usuario en la sesión (cookie) actual.</summary>
    protected Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    protected void AddErrors<T>(Result<T> result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error);
    }
}
