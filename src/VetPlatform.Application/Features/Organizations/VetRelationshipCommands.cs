namespace VetPlatform.Application.Features.Organizations;

public record AssignAttendingVetCommand(Guid PetId, Guid VetUserId, Guid ClinicId);
public record RevokeAttendingVetCommand(Guid PetId);
