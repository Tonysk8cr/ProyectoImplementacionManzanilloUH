using VetPlatform.Application.Features.Clinical;

namespace VetPlatform.API.ViewModels;

/// <summary>Vista de detalle de mascota: datos básicos + historial clínico (PBI-04).</summary>
public class PetDetailsViewModel
{
    public PetDto Pet { get; set; } = default!;
    public ClinicalRecordDto? History { get; set; }
}
