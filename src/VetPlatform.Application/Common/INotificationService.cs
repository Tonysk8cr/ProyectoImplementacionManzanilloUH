namespace VetPlatform.Application.Common;

/// <summary>Decoupled notifications: Enqueue only stages a Pending notification in the current unit of work.</summary>
public interface INotificationService
{
    Task EnqueueAsync(Guid recipientUserId, string subject, string body, Guid? relatedEntityId = null, string? relatedEntityType = null, CancellationToken ct = default);
    Task<int> RetryPendingNotificationsAsync(CancellationToken ct = default);
}

public interface INotificationSender
{
    Task SendAsync(Guid recipientUserId, string subject, string body, CancellationToken ct = default);
}
