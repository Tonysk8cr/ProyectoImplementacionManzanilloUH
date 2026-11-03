using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Enums;
using VetPlatform.Domain.Interfaces.Repositories;

namespace VetPlatform.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _user;
    private readonly TimeProvider _time;

    public AuditService(IAuditLogRepository repo, IUnitOfWork uow, ICurrentUserService user, TimeProvider time)
    { _repo = repo; _uow = uow; _user = user; _time = time; }

    public async Task LogAsync(Guid actorId, AuditAction action, string resourceType, string resourceId, Guid? orgId, string result, string? context = null, CancellationToken ct = default)
    {
        // Context is truncated metadata only; callers must never pass clinical content.
        await _repo.AddAsync(new AuditLog
        {
            ActorUserId = actorId, Action = action, ResourceType = resourceType, ResourceId = resourceId,
            OrganizationId = orgId, Result = result, Timestamp = _time.GetUtcNow(),
            Context = context is { Length: > 500 } ? context[..500] : context,
        }, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<Result<PagedResult<AuditLogDto>>> SearchAsync(string? resourceType, string? resourceId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct = default)
    {
        if (_user.UserId is not { } actor || !_user.IsInRole(UserRole.PlatformAdmin))
            return Result<PagedResult<AuditLogDto>>.Forbidden();
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        var (items, total) = await _repo.SearchAsync(resourceType, resourceId, from, to, page, pageSize, ct);
        // The audit query itself is attributed to the admin.
        await LogAsync(actor, AuditAction.Read, "AuditLog", resourceId ?? "*", null, AuditLog.Results.Allowed, $"filter:type={resourceType}", ct);
        var dtos = items.Select(a => new AuditLogDto(a.Id, a.ActorUserId, a.Action.ToString(), a.ResourceType, a.ResourceId, a.OrganizationId, a.Result, a.Timestamp, a.Context)).ToList();
        return Result<PagedResult<AuditLogDto>>.Ok(new PagedResult<AuditLogDto>(dtos, total, page, pageSize));
    }
}
