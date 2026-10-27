namespace VetPlatform.Application.Features.Organizations;

public record ClinicMemberDto(Guid Id, Guid UserId, DateTimeOffset JoinedAt, DateTimeOffset? LeftAt);
public record ClinicDto(Guid Id, string Name, string Status, Guid RepresentativeUserId, string Address, string Phone, DateTimeOffset CreatedAt, IReadOnlyList<ClinicMemberDto> Members);
public record AttendingVetRelationshipDto(Guid Id, Guid PetId, Guid VetUserId, Guid? ClinicId, DateTimeOffset EstablishedAt, DateTimeOffset? RevokedAt, bool IsActive);
