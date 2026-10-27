using VetPlatform.Application.Common;
namespace VetPlatform.Application.Features.Identity;

public interface IAuthService
{
    Task<Result<UserDto>> RegisterAsync(RegisterUserCommand cmd, CancellationToken ct = default);
    Task<Result<LoginResponse>> LoginAsync(LoginCommand cmd, CancellationToken ct = default);
    Task<Result<UserDto>> SuspendAsync(SuspendUserCommand cmd, CancellationToken ct = default);
    Task<Result<UserDto>> VerifyAsync(Guid userId, CancellationToken ct = default);
}
