namespace VetPlatform.Application.Features.Clinical;

public record PrescriptionDto(Guid Id, Guid ConsultationId, Guid VetUserId, string Medication, string Dosage, string Route,
    string Frequency, int DurationDays, DateTimeOffset CreatedAt, IReadOnlyList<AddendumDto> Addenda);
