using System.ComponentModel.DataAnnotations;
using VetPlatform.Application.Features.Clinical;

namespace VetPlatform.API.ViewModels;

public class AppointmentViewModel
{
    [Required(ErrorMessage = "Selecciona una mascota.")]
    public Guid? PetId { get; set; }
    [Required(ErrorMessage = "Selecciona un día.")]
    [DataType(DataType.Date)]
    public DateOnly? Date { get; set; }
    [Required(ErrorMessage = "Selecciona un horario disponible.")]
    public Guid? SlotId { get; set; }
    [Required(ErrorMessage = "Indica el motivo de la visita.")]
    [StringLength(500, ErrorMessage = "El motivo debe tener como máximo 500 caracteres.")]
    public string Reason { get; set; } = "Consulta";
    public IReadOnlyList<PetDto> Pets { get; set; } = Array.Empty<PetDto>();
    public IReadOnlyList<AppointmentSlotOption> Slots { get; set; } = Array.Empty<AppointmentSlotOption>();
    public DateOnly Today { get; set; }
}

public record AppointmentSlotOption(Guid Id, DateTimeOffset StartTime, string Clinic, string Vet);
public record AppointmentConfirmationViewModel(string Pet, string Reason, DateTimeOffset StartTime, string Clinic);
