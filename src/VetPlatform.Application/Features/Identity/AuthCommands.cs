using VetPlatform.Domain.Enums;
namespace VetPlatform.Application.Features.Identity;

/// <summary>Datos del registro de propietario (PBI-01). Identificación y teléfono son los
/// datos mínimos que define el equipo; la dirección es opcional.</summary>
public record RegisterUserCommand(string Email, string Password, string FullName, UserRole Role,
    string Identification, string Phone, string? Address = null);
public record LoginCommand(string Email, string Password);
public record SuspendUserCommand(Guid UserId, string Reason);
