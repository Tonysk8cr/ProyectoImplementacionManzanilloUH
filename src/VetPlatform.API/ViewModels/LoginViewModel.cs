using System.ComponentModel.DataAnnotations;

namespace VetPlatform.API.ViewModels;

/// <summary>Formulario de inicio de sesión.</summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Mantener sesión iniciada")]
    public bool RememberMe { get; set; }

    /// <summary>URL a la que volver tras iniciar sesión (solo rutas locales).</summary>
    public string? ReturnUrl { get; set; }
}
