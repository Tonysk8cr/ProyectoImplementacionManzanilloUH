using VetPlatform.Application.Common;
namespace VetPlatform.Application.Features.Scheduling;

public interface ISchedulingService
{
    Task<Result<AvailabilitySlotDto>> CreateSlotAsync(CreateAvailabilitySlotCommand cmd, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AvailabilitySlotDto>>> GetAvailabilityAsync(Guid? clinicId, Guid? vetId, DateOnly? date, CancellationToken ct = default);
    Task<Result<AppointmentDto>> BookAppointmentAsync(BookAppointmentCommand cmd, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AppointmentDto>>> GetAppointmentsByPetAsync(Guid petId, CancellationToken ct = default);
}
