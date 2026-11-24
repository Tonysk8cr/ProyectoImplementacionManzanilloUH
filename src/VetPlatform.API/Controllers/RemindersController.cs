using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Features.Scheduling;

namespace VetPlatform.API.Controllers;

[Authorize]
public class RemindersController(IReminderService reminders) : ApiControllerBase
{
    [HttpGet("api/reminders")]
    public async Task<IActionResult> Mine(CancellationToken ct) => ToActionResult(await reminders.GetMineAsync(ct));

    [HttpPost("api/care-schedules")]
    public async Task<IActionResult> Schedule(ScheduleCareCommand command, CancellationToken ct) => ToActionResult(await reminders.ScheduleCareAsync(command, ct), 201);

    [HttpDelete("api/care-schedules/{id:guid}")]
    public async Task<IActionResult> CancelCare(Guid id, CancellationToken ct) => ToActionResult(await reminders.CancelCareAsync(id, ct));

    [HttpDelete("api/appointments/{id:guid}")]
    public async Task<IActionResult> CancelAppointment(Guid id, CancellationToken ct) => ToActionResult(await reminders.CancelAppointmentAsync(id, ct));
}
