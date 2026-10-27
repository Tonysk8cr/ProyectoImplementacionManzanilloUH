using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities.Scheduling;

public class AvailabilitySlot : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Guid VetUserId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public bool IsBooked { get; set; }
    /// <summary>Optimistic concurrency token to prevent double booking.</summary>
    public int Version { get; set; }
}
