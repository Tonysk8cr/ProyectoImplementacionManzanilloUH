namespace VetPlatform.Application.Features.Clinical;

public record RegisterPrescriptionCommand(Guid ConsultationId, string Medication, string Dosage, string Route, string Frequency, int DurationDays);
public record AddPrescriptionAddendumCommand(Guid PrescriptionId, string Reason, string Content);
