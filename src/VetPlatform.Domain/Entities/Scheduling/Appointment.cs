using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities.Scheduling;

public class Appointment : BaseEntity
{
    public Guid PetId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid ClinicId { get; set; }
    public Guid VetUserId { get; set; }
    public Guid SlotId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = Statuses.Booked;

    public static class Statuses
    {
        public const string Booked = "Booked";
        public const string Cancelled = "Cancelled";
    }
}
