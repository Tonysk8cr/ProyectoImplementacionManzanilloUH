using System.ComponentModel.DataAnnotations;

namespace VetPlatform.API.ViewModels;

/// <summary>Formulario de datos básicos del propietario (PBI-01). Nombre y correo ya viven en la cuenta.</summary>
public class OwnerProfileViewModel
{
    [Required(ErrorMessage = "La identificación es obligatoria.")]
    [StringLength(20, MinimumLength = 5, ErrorMessage = "La identificación debe tener entre 5 y 20 caracteres.")]
    [RegularExpression("^[0-9A-Za-z-]+$", ErrorMessage = "La identificación solo admite letras, números y guiones.")]
    [Display(Name = "Número de identificación")]
    public string IdentificationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [RegularExpression(@"^\+?[0-9\s-]{8,20}$", ErrorMessage = "El teléfono no es válido (mínimo 8 dígitos).")]
    [Display(Name = "Teléfono")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "La dirección no puede exceder 300 caracteres.")]
    [Display(Name = "Dirección (opcional)")]
    public string? Address { get; set; }
}
