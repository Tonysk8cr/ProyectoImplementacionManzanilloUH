namespace VetPlatform.Application.Features.Identity;

public record LoginResponse(string Token, DateTimeOffset Expiry, Guid UserId, IReadOnlyList<string> Roles);
public record UserDto(Guid Id, string Email, string FullName, bool IsVerified, bool IsSuspended, IReadOnlyList<string> Roles);
