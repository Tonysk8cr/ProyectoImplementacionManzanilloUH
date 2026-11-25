namespace VetPlatform.Domain.Entities.Scheduling;

public sealed record AppointmentDetails(
    Guid Id,
    Guid PetId,
    string PetName,
    Guid OwnerId,
    Guid ClinicId,
    string ClinicName,
    Guid VetUserId,
    Guid SlotId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    DateTimeOffset CreatedAt,
    string Reason,
    string Status);
