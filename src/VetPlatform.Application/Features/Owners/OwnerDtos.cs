namespace VetPlatform.Application.Features.Owners;

public record OwnerProfileDto(Guid Id, Guid UserId, string FullName, string Email, string IdentificationNumber,
    string Phone, string? Address, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
