namespace VetPlatform.Application.Features.Scheduling;

public record CreateAvailabilitySlotCommand(Guid ClinicId, Guid VetUserId, DateTimeOffset StartTime, DateTimeOffset EndTime);
public record BookAppointmentCommand(Guid PetId, Guid SlotId);
