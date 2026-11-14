using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Infrastructure.Data;
using VetPlatform.Domain.Entities.Scheduling;
using VetPlatform.Application.Common;
using VetPlatform.Domain.Entities.Notifications;
using VetPlatform.Domain.Enums;
using VetPlatform.Domain.Interfaces.Repositories;

namespace VetPlatform.Infrastructure.Services;

public class NotificationService : INotificationService
{
    public const int MaxRetries = 5;
    private readonly INotificationRepository _repo;
    private readonly INotificationSender _sender;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;
    private readonly ILogger<NotificationService> _log;
    private readonly AppDbContext _db;

    public NotificationService(INotificationRepository repo, INotificationSender sender, IUnitOfWork uow, TimeProvider time, ILogger<NotificationService> log, AppDbContext db)
    { _repo = repo; _sender = sender; _uow = uow; _time = time; _log = log; _db = db; }

    public Task EnqueueAppointmentReminderAsync(Guid ownerId, Guid appointmentId, string petName, string reason, DateTimeOffset at, CancellationToken ct = default) =>
        _repo.AddAsync(ReminderService.Create(ownerId, appointmentId, ReminderService.AppointmentReminder, petName, reason, at, _time.GetUtcNow()), ct);

    /// <summary>Stages the notification; it is persisted by the caller's SaveChanges (same transaction as the business act).</summary>
    public Task EnqueueAsync(Guid recipientUserId, string subject, string body, Guid? relatedEntityId = null, string? relatedEntityType = null, CancellationToken ct = default) =>
        _repo.AddAsync(new Notification
        {
            RecipientUserId = recipientUserId, Subject = subject, Body = body, Status = NotificationStatus.Pending,
            CreatedAt = _time.GetUtcNow(), RelatedEntityId = relatedEntityId, RelatedEntityType = relatedEntityType,
        }, ct);

    public async Task<int> RetryPendingNotificationsAsync(CancellationToken ct = default)
    {
        var pending = await _repo.GetPendingAsync(MaxRetries, 100, ct);
        var sent = 0;
        foreach (var n in pending)
        {
            ct.ThrowIfCancellationRequested();
            if (n.EventAt.HasValue && n.EventAt <= _time.GetUtcNow())
            {
                n.Status = NotificationStatus.Cancelled;
                continue;
            }
            if (n.RelatedEntityType == ReminderService.AppointmentReminder &&
                !await _db.Appointments.AsNoTracking().AnyAsync(a => a.Id == n.RelatedEntityId && a.Status == Appointment.Statuses.Booked, ct))
            {
                n.Status = NotificationStatus.Cancelled;
                continue;
            }
            try
            {
                await _sender.SendAsync(n.RecipientUserId, n.Subject, n.Body, ct);
                n.Status = NotificationStatus.Sent;
                n.SentAt = _time.GetUtcNow();
                sent++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                n.RetryCount++;
                n.Status = NotificationStatus.Failed;
                _log.LogWarning(ex, "Notification {Id} failed (attempt {Attempt})", n.Id, n.RetryCount);
            }
        }
        await _uow.SaveChangesAsync(ct);
        return sent;
    }
}

/// <summary>Default sender: logs. Replace with email/push provider.</summary>
public class LoggingNotificationSender : INotificationSender
{
    private readonly ILogger<LoggingNotificationSender> _log;
    public LoggingNotificationSender(ILogger<LoggingNotificationSender> log) => _log = log;
    public Task SendAsync(Guid recipientUserId, string subject, string body, CancellationToken ct = default)
    {
        _log.LogInformation("Notification to {User}: {Subject}", recipientUserId, subject);
        return Task.CompletedTask;
    }
}

/// <summary>Background dispatcher that decouples delivery from the business transaction.</summary>
public class NotificationDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<NotificationDispatcherHostedService> _log;
    public NotificationDispatcherHostedService(IServiceScopeFactory scopes, ILogger<NotificationDispatcherHostedService> log) { _scopes = scopes; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<INotificationService>().RetryPendingNotificationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { _log.LogError(ex, "Notification dispatch failed"); }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
