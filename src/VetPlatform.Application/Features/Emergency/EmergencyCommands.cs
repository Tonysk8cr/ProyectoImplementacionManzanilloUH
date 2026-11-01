namespace VetPlatform.Application.Features.Emergency;

public record RequestEmergencyAccessCommand(Guid PetId, Guid VetUserId, string Reason);
public record RegisterEmergencyAttendanceCommand(Guid EmergencyAccessId, DateTimeOffset AttendanceDate, string Findings, string Actions, string? Instructions);
