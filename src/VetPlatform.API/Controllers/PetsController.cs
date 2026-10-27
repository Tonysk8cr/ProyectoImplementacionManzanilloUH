using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.API.Controllers;

[Route("api/pets"), Authorize]
public class PetsController : ApiControllerBase
{
    private readonly IClinicalService _clinical;
    public PetsController(IClinicalService clinical) => _clinical = clinical;

    [Authorize(Policy = AuthorizationPolicies.RequirePetOwner), HttpPost]
    public async Task<IActionResult> Create(CreatePetCommand cmd, CancellationToken ct) =>
        ToActionResult(await _clinical.CreatePetAsync(cmd with { OwnerId = CurrentUserId }, ct), 201);

    /// <summary>Owner, active attending vet or emergency vet with valid access (checked in service).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => ToActionResult(await _clinical.GetPetAsync(id, ct));

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, CancellationToken ct) => ToActionResult(await _clinical.GetHistoryAsync(id, ct));
}
