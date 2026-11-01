using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Entities.Clinical;
using VetPlatform.Domain.Entities.Emergency;
using VetPlatform.Domain.Entities.Identity;
using VetPlatform.Domain.Entities.Marketplace;
using VetPlatform.Domain.Entities.Notifications;
using VetPlatform.Domain.Entities.Organizations;
using VetPlatform.Domain.Entities.Scheduling;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Domain.Interfaces.Repositories;

public interface IAuditLogRepository : IBaseRepository<AuditLog>
{
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(
        string? resourceType, string? resourceId, DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, CancellationToken ct = default);
}
