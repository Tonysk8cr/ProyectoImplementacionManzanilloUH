using VetPlatform.Domain.Common;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Domain.Entities.Clinical;

/// <summary>Immutable once confirmed. Corrections are recorded as <see cref="Addendum"/>.</summary>
public class Consultation : BaseEntity, IImmutableRecord
{
    public Guid ClinicalRecordId { get; set; }
    public Guid VetUserId { get; set; }
    public DateTimeOffset AttendanceDate { get; set; }
    public DateTimeOffset RegistrationDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public ConsultationStatus Status { get; set; } = ConsultationStatus.Confirmed;
    public string SourceType { get; set; } = SourceTypes.Routine;

    public ICollection<Addendum> Addenda { get; set; } = new List<Addendum>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public static class SourceTypes
    {
        public const string Routine = "Routine";
        public const string Emergency = "Emergency";
    }
}
