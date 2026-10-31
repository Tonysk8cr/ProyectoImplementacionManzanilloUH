using VetPlatform.Domain.Common;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Domain.Entities.Notifications;

public class Notification : BaseEntity
{
    public Guid RecipientUserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public int RetryCount { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}
