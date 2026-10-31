using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.API.Controllers;

[Route("api/consultations"), Authorize]
public class ConsultationsController : ApiControllerBase
{
    private readonly IClinicalService _clinical;
    public ConsultationsController(IClinicalService clinical) => _clinical = clinical;

    [Authorize(Policy = AuthorizationPolicies.RequireAttendingVetAccess), HttpPost]
    public async Task<IActionResult> Register(RegisterConsultationCommand cmd, CancellationToken ct) =>
        ToActionResult(await _clinical.RegisterConsultationAsync(cmd with { VetUserId = CurrentUserId }, ct), 201);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => ToActionResult(await _clinical.GetConsultationAsync(id, ct));

    public record AddendumRequest(string Reason, string Content);

    [Authorize(Policy = AuthorizationPolicies.RequireAttendingVetAccess), HttpPost("{id:guid}/addenda")]
    public async Task<IActionResult> AddAddendum(Guid id, AddendumRequest body, CancellationToken ct) =>
        ToActionResult(await _clinical.AddAddendumAsync(new AddAddendumCommand(id, body.Reason, body.Content), ct), 201);
}
