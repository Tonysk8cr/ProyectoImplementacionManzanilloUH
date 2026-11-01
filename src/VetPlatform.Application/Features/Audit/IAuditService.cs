using VetPlatform.Application.Common;
using VetPlatform.Domain.Enums;
namespace VetPlatform.Application.Features.Audit;

/// <summary>Audit entries carry only metadata (actor, action, resource, result). Never clinical content.</summary>
public interface IAuditService
{
    Task LogAsync(Guid actorId, AuditAction action, string resourceType, string resourceId, Guid? orgId, string result, string? context = null, CancellationToken ct = default);
    Task<Result<PagedResult<AuditLogDto>>> SearchAsync(string? resourceType, string? resourceId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct = default);
}
