namespace VetPlatform.Application.Features.Clinical;

public record AddendumDto(Guid Id, Guid AuthorUserId, string Reason, string Content, DateTimeOffset CreatedAt);
public record ConsultationDto(Guid Id, Guid ClinicalRecordId, Guid VetUserId, DateTimeOffset AttendanceDate, DateTimeOffset RegistrationDate,
    string Notes, string? Instructions, string Status, string SourceType, IReadOnlyList<AddendumDto> Addenda, IReadOnlyList<PrescriptionDto> Prescriptions);
