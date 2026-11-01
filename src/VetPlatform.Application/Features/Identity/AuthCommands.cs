using VetPlatform.Domain.Enums;
namespace VetPlatform.Application.Features.Identity;

public record RegisterUserCommand(string Email, string Password, string FullName, UserRole Role);
public record LoginCommand(string Email, string Password);
public record SuspendUserCommand(Guid UserId, string Reason);
