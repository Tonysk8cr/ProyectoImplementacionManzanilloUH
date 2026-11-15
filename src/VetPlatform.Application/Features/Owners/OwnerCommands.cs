namespace VetPlatform.Application.Features.Owners;

public record CreateOwnerProfileCommand(string IdentificationNumber, string Phone, string? Address);
public record UpdateOwnerProfileCommand(string IdentificationNumber, string Phone, string? Address);

/// <summary>Registro de un propietario: crea la cuenta (correo y contraseña) y sus datos básicos en un solo paso.</summary>
public record RegisterOwnerCommand(string Email, string Password, string FullName,
    string IdentificationNumber, string Phone, string? Address);
