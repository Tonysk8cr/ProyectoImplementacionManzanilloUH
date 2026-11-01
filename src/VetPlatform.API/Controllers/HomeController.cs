using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.API.ViewModels;

namespace VetPlatform.API.Controllers;

public class HomeController : Controller
{
    /// <summary>Portada de la aplicación.</summary>
    [AllowAnonymous]
    public IActionResult Index() => View();

    /// <summary>Página de error (destino de UseExceptionHandler y de los códigos 4xx/5xx).</summary>
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
    });
}
