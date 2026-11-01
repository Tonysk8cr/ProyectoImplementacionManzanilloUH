using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities.Organizations;

public class ClinicMember : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; }

    public bool IsActive => LeftAt == null;
}
