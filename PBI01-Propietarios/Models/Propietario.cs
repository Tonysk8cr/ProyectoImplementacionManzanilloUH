using System.ComponentModel.DataAnnotations;

namespace PBI01.Propietarios.Models;

/// <summary>Datos básicos del propietario de mascotas (PBI-01).</summary>
public class Propietario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 200 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "La identificación es obligatoria.")]
    [StringLength(20, MinimumLength = 5, ErrorMessage = "La identificación debe tener entre 5 y 20 caracteres.")]
    [RegularExpression("^[0-9A-Za-z-]+$", ErrorMessage = "La identificación solo admite letras, números y guiones.")]
    [Display(Name = "Número de identificación")]
    public string Identificacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(256)]
    [Display(Name = "Correo electrónico")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [RegularExpression(@"^\+?[0-9\s-]{8,20}$", ErrorMessage = "El teléfono no es válido (mínimo 8 dígitos).")]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "La dirección no puede exceder 300 caracteres.")]
    [Display(Name = "Dirección (opcional)")]
    public string? Direccion { get; set; }

    [Display(Name = "Registrado el")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
