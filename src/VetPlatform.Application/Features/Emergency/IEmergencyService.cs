using VetPlatform.Application.Common;
namespace VetPlatform.Application.Features.Emergency;

public interface IEmergencyService
{
    Task<Result<PetSummaryForEmergencyDto>> ResolvePetAsync(string qrOrId, CancellationToken ct = default);
    Task<Result<EmergencyAccessDto>> RequestAccessAsync(RequestEmergencyAccessCommand cmd, CancellationToken ct = default);
    Task<Result<EmergencyAttendanceDto>> RegisterAttendanceAsync(RegisterEmergencyAttendanceCommand cmd, CancellationToken ct = default);
}
