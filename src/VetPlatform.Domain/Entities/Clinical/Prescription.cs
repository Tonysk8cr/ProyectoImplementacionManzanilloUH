using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities.Clinical;

public class Prescription : BaseEntity, IImmutableRecord
{
    public Guid ConsultationId { get; set; }
    public Guid VetUserId { get; set; }
    public string Medication { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<PrescriptionAddendum> Addenda { get; set; } = new List<PrescriptionAddendum>();
}
