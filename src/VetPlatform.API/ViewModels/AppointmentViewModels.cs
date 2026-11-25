using System.ComponentModel.DataAnnotations;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Application.Features.Organizations;
using VetPlatform.Application.Features.Scheduling;

namespace VetPlatform.API.ViewModels;

public sealed class AppointmentBookingViewModel
{
    [Required(ErrorMessage = "Seleccione una veterinaria.")]
    public Guid? ClinicId { get; set; }

    [Required(ErrorMessage = "Seleccione una mascota.")]
    public Guid? PetId { get; set; }

    [Required(ErrorMessage = "Seleccione una fecha.")]
    [DataType(DataType.Date)]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Seleccione una fecha y un horario disponible.")]
    public Guid? SlotId { get; set; }

    [Required(ErrorMessage = "Indique el motivo de la consulta.")]
    [StringLength(500, ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
    public string? Reason { get; set; }

    public IReadOnlyList<PetDto> Pets { get; set; } = Array.Empty<PetDto>();
    public IReadOnlyList<ClinicDto> Clinics { get; set; } = Array.Empty<ClinicDto>();
    public IReadOnlyList<AvailabilitySlotDto> Slots { get; set; } = Array.Empty<AvailabilitySlotDto>();
}

public sealed class AppointmentsIndexViewModel
{
    public IReadOnlyList<AppointmentDto> Appointments { get; set; } = Array.Empty<AppointmentDto>();
    public IReadOnlyList<ClinicDto> Clinics { get; set; } = Array.Empty<ClinicDto>();
    public Guid? SelectedClinicId { get; set; }
    public bool IsClinicManager { get; set; }
}
