using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities.Emergency;

public class EmergencyAttendance : BaseEntity, IImmutableRecord
{
    public Guid EmergencyAccessId { get; set; }
    public Guid ClinicalRecordId { get; set; }
    public Guid VetUserId { get; set; }
    public DateTimeOffset AttendanceDate { get; set; }
    public DateTimeOffset RegistrationDate { get; set; }
    public string Findings { get; set; } = string.Empty;
    public string Actions { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
