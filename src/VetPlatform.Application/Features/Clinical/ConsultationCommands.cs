namespace VetPlatform.Application.Features.Clinical;

public record RegisterConsultationCommand(Guid ClinicalRecordId, Guid VetUserId, DateTimeOffset AttendanceDate, string Notes, string? Instructions);
public record AddAddendumCommand(Guid ConsultationId, string Reason, string Content);
