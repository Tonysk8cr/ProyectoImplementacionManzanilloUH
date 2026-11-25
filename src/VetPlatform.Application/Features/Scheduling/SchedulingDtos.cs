namespace VetPlatform.Application.Features.Scheduling;

public record AvailabilitySlotDto(Guid Id, Guid ClinicId, Guid VetUserId, DateTimeOffset StartTime, DateTimeOffset EndTime, bool IsBooked);
public record AppointmentDto(
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
