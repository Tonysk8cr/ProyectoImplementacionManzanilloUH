using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Features.Identity;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.API.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [AllowAnonymous, HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserCommand cmd, CancellationToken ct) => ToActionResult(await _auth.RegisterAsync(cmd, ct), 201);

    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand cmd, CancellationToken ct) => ToActionResult(await _auth.LoginAsync(cmd, ct));

    public record SuspendRequest(string Reason);

    [Authorize(Policy = AuthorizationPolicies.RequirePlatformAdmin), HttpPost("suspend/{userId:guid}")]
    public async Task<IActionResult> Suspend(Guid userId, SuspendRequest body, CancellationToken ct) =>
        ToActionResult(await _auth.SuspendAsync(new SuspendUserCommand(userId, body.Reason), ct));

    [Authorize(Policy = AuthorizationPolicies.RequirePlatformAdmin), HttpPost("verify/{userId:guid}")]
    public async Task<IActionResult> Verify(Guid userId, CancellationToken ct) => ToActionResult(await _auth.VerifyAsync(userId, ct));
}
