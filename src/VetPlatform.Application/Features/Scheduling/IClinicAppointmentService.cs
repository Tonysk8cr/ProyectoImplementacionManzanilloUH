using VetPlatform.Application.Common;

namespace VetPlatform.Application.Features.Scheduling;

public record ClinicOptionDto(Guid Id, string Name);
public record ClinicAppointmentDto
{
    public Guid Id { get; init; }
    public Guid ClinicId { get; init; }
    public string Clinic { get; init; } = string.Empty;
    public Guid PetId { get; init; }
    public string Pet { get; init; } = string.Empty;
    public string Client { get; init; } = string.Empty;
    public string Vet { get; init; } = string.Empty;
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<string> AllowedStatuses { get; init; } = Array.Empty<string>();
}

public interface IClinicAppointmentService
{
    Task<Result<IReadOnlyList<ClinicOptionDto>>> GetClinicsAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<ClinicAppointmentDto>>> SearchAsync(Guid clinicId, DateOnly? date, string? status, CancellationToken ct = default);
    Task<Result<ClinicAppointmentDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<bool>> ChangeStatusAsync(Guid id, string status, CancellationToken ct = default);
}
