using VetPlatform.Application.Common;
namespace VetPlatform.Application.Features.Organizations;

public interface IClinicService
{
    Task<Result<ClinicDto>> CreateClinicAsync(CreateClinicCommand cmd, CancellationToken ct = default);
    Task<Result<ClinicDto>> GetClinicAsync(Guid id, CancellationToken ct = default);
    Task<Result<ClinicMemberDto>> AddMemberAsync(AddMemberCommand cmd, CancellationToken ct = default);
    Task<Result<bool>> RemoveMemberAsync(RemoveMemberCommand cmd, CancellationToken ct = default);
}
