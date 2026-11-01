namespace VetPlatform.Application.Features.Clinical;

public record PetDto(Guid Id, Guid OwnerId, string Name, string Species, string? Breed, DateOnly? BirthDate, string QrCode, DateTimeOffset CreatedAt);
public record EmergencyAttendanceSummaryDto(Guid Id, Guid VetUserId, DateTimeOffset AttendanceDate, string Findings, string Actions, string? Instructions);
public record ClinicalRecordDto(Guid Id, Guid PetId, DateTimeOffset CreatedAt, IReadOnlyList<ConsultationDto> Consultations, IReadOnlyList<EmergencyAttendanceSummaryDto> EmergencyAttendances);
