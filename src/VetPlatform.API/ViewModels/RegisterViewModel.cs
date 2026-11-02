using System.ComponentModel.DataAnnotations;

namespace VetPlatform.API.ViewModels;

/// <summary>Formulario de registro de propietario (PBI-01).</summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 200 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La identificación es obligatoria.")]
    [StringLength(20, MinimumLength = 5, ErrorMessage = "La identificación debe tener entre 5 y 20 caracteres.")]
    [RegularExpression("^[0-9A-Za-z-]+$", ErrorMessage = "La identificación solo admite letras, números y guiones.")]
    [Display(Name = "Número de identificación")]
    public string Identification { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [RegularExpression(@"^\+?[0-9\s-]{8,20}$", ErrorMessage = "El teléfono no es válido (mínimo 8 dígitos).")]
    [Display(Name = "Teléfono")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "La dirección no puede exceder 300 caracteres.")]
    [Display(Name = "Dirección (opcional)")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(256)]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).*$",
        ErrorMessage = "La contraseña debe tener al menos una mayúscula y un número.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
