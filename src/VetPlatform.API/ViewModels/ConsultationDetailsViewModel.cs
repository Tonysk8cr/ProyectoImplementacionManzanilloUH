using VetPlatform.Application.Features.Clinical;

namespace VetPlatform.API.ViewModels;

/// <summary>Detalle de una consulta junto con el paciente al que pertenece.</summary>
public class ConsultationDetailsViewModel
{
    public PetDto Pet { get; set; } = default!;
    public ConsultationDto? Consultation { get; set; }
}
