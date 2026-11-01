namespace VetPlatform.Application.Features.Scheduling;

public record AvailabilitySlotDto(Guid Id, Guid ClinicId, Guid VetUserId, DateTimeOffset StartTime, DateTimeOffset EndTime, bool IsBooked);
public record AppointmentDto(Guid Id, Guid PetId, Guid OwnerId, Guid ClinicId, Guid VetUserId, Guid SlotId, DateTimeOffset CreatedAt, string Status);
