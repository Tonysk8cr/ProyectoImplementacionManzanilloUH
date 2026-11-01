using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Features.Organizations;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.API.Controllers;

[Authorize]
public class ClinicsController : ApiControllerBase
{
    private readonly IClinicService _clinics;
    private readonly IVetRelationshipService _relationships;
    public ClinicsController(IClinicService clinics, IVetRelationshipService relationships) { _clinics = clinics; _relationships = relationships; }

    [Authorize(Policy = AuthorizationPolicies.RequirePlatformAdmin), HttpPost("api/clinics")]
    public async Task<IActionResult> Create(CreateClinicCommand cmd, CancellationToken ct) => ToActionResult(await _clinics.CreateClinicAsync(cmd, ct), 201);

    [HttpGet("api/clinics/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => ToActionResult(await _clinics.GetClinicAsync(id, ct));

    public record AddMemberRequest(Guid UserId);

    [Authorize(Policy = AuthorizationPolicies.RequireClinicAdmin), HttpPost("api/clinics/{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, AddMemberRequest body, CancellationToken ct) =>
        ToActionResult(await _clinics.AddMemberAsync(new AddMemberCommand(id, body.UserId), ct), 201);

    [Authorize(Policy = AuthorizationPolicies.RequireClinicAdmin), HttpDelete("api/clinics/{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct) =>
        ToActionResult(await _clinics.RemoveMemberAsync(new RemoveMemberCommand(id, userId), ct));

    public record AssignVetRequest(Guid PetId, Guid VetUserId);

    [Authorize(Policy = AuthorizationPolicies.RequirePetOwner), HttpPost("api/clinics/{id:guid}/attending-vet")]
    public async Task<IActionResult> AssignAttendingVet(Guid id, AssignVetRequest body, CancellationToken ct) =>
        ToActionResult(await _relationships.AssignAsync(new AssignAttendingVetCommand(body.PetId, body.VetUserId, id), ct), 201);

    [Authorize(Policy = AuthorizationPolicies.RequirePetOwner), HttpDelete("api/pets/{petId:guid}/attending-vet")]
    public async Task<IActionResult> RevokeAttendingVet(Guid petId, CancellationToken ct) =>
        ToActionResult(await _relationships.RevokeAsync(new RevokeAttendingVetCommand(petId), ct));
}
