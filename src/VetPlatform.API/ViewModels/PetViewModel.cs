using System.ComponentModel.DataAnnotations;

namespace VetPlatform.API.ViewModels;

/// <summary>Formulario de registro de mascota (PBI-02). El vínculo con el dueño (PBI-03) se hace en el servidor.</summary>
public class PetViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La especie es obligatoria.")]
    [StringLength(50, ErrorMessage = "La especie no puede exceder 50 caracteres.")]
    [Display(Name = "Especie")]
    public string Species { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "La raza no puede exceder 100 caracteres.")]
    [Display(Name = "Raza (opcional)")]
    public string? Breed { get; set; }

    [Display(Name = "Fecha de nacimiento (opcional)")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateOnly? BirthDate { get; set; }
}
