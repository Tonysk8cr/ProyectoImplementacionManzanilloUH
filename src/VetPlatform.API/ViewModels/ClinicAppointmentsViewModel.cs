using VetPlatform.Application.Features.Scheduling;

namespace VetPlatform.API.ViewModels;

public class ClinicAppointmentsViewModel
{
    public Guid? ClinicId { get; set; }
    public DateOnly? Date { get; set; }
    public string? Status { get; set; }
    public IReadOnlyList<ClinicOptionDto> Clinics { get; set; } = Array.Empty<ClinicOptionDto>();
    public IReadOnlyList<ClinicAppointmentDto> Appointments { get; set; } = Array.Empty<ClinicAppointmentDto>();

    public static string StatusLabel(string status) => status switch
    {
        "Booked" => "Reservada", "Cancelled" => "Cancelada", "Completed" => "Completada", "NoShow" => "Ausente", _ => status
    };
    public static string ActionLabel(string status) => status switch
    {
        "Cancelled" => "Cancelar cita", "Completed" => "Marcar completada", "NoShow" => "Marcar ausente", _ => status
    };
}
