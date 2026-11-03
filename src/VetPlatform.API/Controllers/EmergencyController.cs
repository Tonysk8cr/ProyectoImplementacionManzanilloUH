using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Features.Emergency;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.API.Controllers;

[Route("api/emergency"), Authorize]
public class EmergencyController : ApiControllerBase
{
    private readonly IEmergencyService _emergency;
    public EmergencyController(IEmergencyService emergency) => _emergency = emergency;

    [Authorize(Policy = AuthorizationPolicies.RequireVet), HttpGet("resolve/{qrOrId}")]
    public async Task<IActionResult> Resolve(string qrOrId, CancellationToken ct) => ToActionResult(await _emergency.ResolvePetAsync(qrOrId, ct));

    [Authorize(Policy = AuthorizationPolicies.RequireEmergencyAccess), HttpPost("access")]
    public async Task<IActionResult> RequestAccess(RequestEmergencyAccessCommand cmd, CancellationToken ct) =>
        ToActionResult(await _emergency.RequestAccessAsync(cmd with { VetUserId = CurrentUserId }, ct), 201);

    [Authorize(Policy = AuthorizationPolicies.RequireEmergencyAccess), HttpPost("attendance")]
    public async Task<IActionResult> RegisterAttendance(RegisterEmergencyAttendanceCommand cmd, CancellationToken ct) =>
        ToActionResult(await _emergency.RegisterAttendanceAsync(cmd, ct), 201);
}
