using VetPlatform.Application.Common;
namespace VetPlatform.Application.Features.Organizations;

public interface IVetRelationshipService
{
    Task<Result<AttendingVetRelationshipDto>> AssignAsync(AssignAttendingVetCommand cmd, CancellationToken ct = default);
    Task<Result<bool>> RevokeAsync(RevokeAttendingVetCommand cmd, CancellationToken ct = default);
}
