using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Enums;

namespace VetPlatform.API.Middleware;

/// <summary>Automatically audits 401/403 responses (path only, never request bodies).</summary>
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    public AuditMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, IAuditService audit, ICurrentUserService user)
    {
        await _next(ctx);
        if (ctx.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            var segments = ctx.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            var resourceType = segments.Length > 1 ? segments[1] : "unknown";
            try
            {
                await audit.LogAsync(user.UserId ?? Guid.Empty, AuditAction.AccessDenied, resourceType, ctx.Request.Path.Value ?? "",
                    user.OrganizationId, AuditLog.Results.Denied, $"{ctx.Request.Method} status={ctx.Response.StatusCode}");
            }
            catch { /* auditing must never break the response */ }
        }
    }
}
