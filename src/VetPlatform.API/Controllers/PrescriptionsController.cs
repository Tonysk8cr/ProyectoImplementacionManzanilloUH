using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.API.Controllers;

[Route("api/prescriptions"), Authorize]
public class PrescriptionsController : ApiControllerBase
{
    private readonly IClinicalService _clinical;
    public PrescriptionsController(IClinicalService clinical) => _clinical = clinical;

    [Authorize(Policy = AuthorizationPolicies.RequireAttendingVetAccess), HttpPost]
    public async Task<IActionResult> Register(RegisterPrescriptionCommand cmd, CancellationToken ct) =>
        ToActionResult(await _clinical.RegisterPrescriptionAsync(cmd, ct), 201);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => ToActionResult(await _clinical.GetPrescriptionAsync(id, ct));

    [Authorize(Policy = AuthorizationPolicies.RequireAttendingVetAccess), HttpPost("{id:guid}/addenda")]
    public async Task<IActionResult> AddAddendum(Guid id, ConsultationsController.AddendumRequest body, CancellationToken ct) =>
        ToActionResult(await _clinical.AddPrescriptionAddendumAsync(new AddPrescriptionAddendumCommand(id, body.Reason, body.Content), ct), 201);
}
