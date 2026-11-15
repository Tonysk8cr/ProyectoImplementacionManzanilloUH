using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.API.ViewModels;
using VetPlatform.Application.Features.Identity;
using VetPlatform.Application.Features.Owners;
using VetPlatform.Domain.Enums;

namespace VetPlatform.API.Controllers;

/// <summary>
/// Registro e inicio de sesión del propietario (PBI-01). La sesión se guarda en una cookie
/// de autenticación; el JWT que genera el servicio queda disponible por si se retoma una API.
/// </summary>
public class AccountController : MvcControllerBase
{
    private readonly IAuthService _authService;
    private readonly IOwnerService _owners;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthService authService, IOwnerService owners, ILogger<AccountController> logger)
    {
        _authService = authService;
        _owners = owners;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Pets");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Sprint 1: el registro público crea siempre un propietario de mascotas
        // (cuenta + cédula, teléfono y dirección en un solo paso).
        var result = await _owners.RegisterOwnerAsync(
            new RegisterOwnerCommand(model.Email, model.Password, model.FullName,
                model.IdentificationNumber, model.Phone, model.Address));

        if (!result.Success)
        {
            AddErrors(result);
            return View(model);
        }

        var user = result.Value;
        if (user is null)
        {
            return View(model);
        }

        _logger.LogInformation("Propietario {UserId} ({Email}) registrado", user.Id, user.Email);
        TempData["SuccessMessage"] = "Cuenta creada correctamente. Ahora puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Pets");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.LoginAsync(new LoginCommand(model.Email, model.Password));

        if (!result.Success)
        {
            AddErrors(result);
            return View(model);
        }

        var session = result.Value;
        if (session is null)
        {
            ModelState.AddModelError(string.Empty, "No se pudo iniciar sesión.");
            return View(model);
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CreateClaimsPrincipal(session),
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        _logger.LogInformation("Usuario {UserId} ({Email}) inició sesión", session.UserId, session.Email);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Pets");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    /// <summary>Convierte la respuesta de login en la identidad de la cookie del navegador.</summary>
    private static ClaimsPrincipal CreateClaimsPrincipal(LoginResponse session)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new(ClaimTypes.Name, session.FullName),
            new(ClaimTypes.Email, session.Email)
        };
        claims.AddRange(session.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
