namespace VetPlatform.Application.Features.Organizations;

public record CreateClinicCommand(string Name, Guid RepresentativeUserId, string Address, string Phone);
public record AddMemberCommand(Guid ClinicId, Guid UserId);
public record RemoveMemberCommand(Guid ClinicId, Guid UserId);
