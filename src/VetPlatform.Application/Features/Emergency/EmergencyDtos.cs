namespace VetPlatform.Application.Features.Emergency;

public record EmergencyAccessDto(Guid Id, Guid PetId, Guid VetUserId, string Reason, DateTimeOffset GrantedAt, DateTimeOffset ExpiresAt, string Status);
public record EmergencyAttendanceDto(Guid Id, Guid EmergencyAccessId, Guid ClinicalRecordId, Guid VetUserId, DateTimeOffset AttendanceDate,
    DateTimeOffset RegistrationDate, string Findings, string Actions, string? Instructions);
/// <summary>Minimal, non-clinical data needed to identify the pet in an emergency.</summary>
public record PetSummaryForEmergencyDto(Guid PetId, string Name, string Species, string? Breed, bool HasAttendingVet);
