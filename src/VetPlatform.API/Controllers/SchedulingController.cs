using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Features.Scheduling;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.API.Controllers;

[Authorize]
public class SchedulingController : ApiControllerBase
{
    private readonly ISchedulingService _scheduling;
    public SchedulingController(ISchedulingService scheduling) => _scheduling = scheduling;

    [Authorize(Policy = AuthorizationPolicies.RequireClinicAdmin), HttpPost("api/availability")]
    public async Task<IActionResult> CreateSlot(CreateAvailabilitySlotCommand cmd, CancellationToken ct) => ToActionResult(await _scheduling.CreateSlotAsync(cmd, ct), 201);

    [HttpGet("api/availability")]
    public async Task<IActionResult> Availability([FromQuery] Guid? clinicId, [FromQuery] Guid? vetId, [FromQuery] DateOnly? date, CancellationToken ct) =>
        ToActionResult(await _scheduling.GetAvailabilityAsync(clinicId, vetId, date, ct));

    [Authorize(Policy = AuthorizationPolicies.RequirePetOwner), HttpPost("api/appointments")]
    public async Task<IActionResult> Book(BookAppointmentCommand cmd, CancellationToken ct) => ToActionResult(await _scheduling.BookAppointmentAsync(cmd, ct), 201);

    [HttpGet("api/appointments")]
    public async Task<IActionResult> ByPet([FromQuery] Guid petId, CancellationToken ct) => ToActionResult(await _scheduling.GetAppointmentsByPetAsync(petId, ct));
}
