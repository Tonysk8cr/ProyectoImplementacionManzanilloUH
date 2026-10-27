using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.API.Controllers;

[Route("api/audit"), Authorize(Policy = AuthorizationPolicies.RequirePlatformAdmin)]
public class AuditController : ApiControllerBase
{
    private readonly IAuditService _audit;
    public AuditController(IAuditService audit) => _audit = audit;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? resourceType, [FromQuery] string? resourceId, [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        ToActionResult(await _audit.SearchAsync(resourceType, resourceId, from, to, page, pageSize, ct));
}
