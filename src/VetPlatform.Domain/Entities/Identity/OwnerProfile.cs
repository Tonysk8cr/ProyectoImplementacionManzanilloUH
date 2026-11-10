using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities.Identity;

/// <summary>
/// Datos básicos del propietario de mascotas (PBI-01). Complementa a <see cref="User"/> (que ya guarda
/// nombre y correo) con los datos mínimos de identificación y contacto. Un usuario tiene un solo perfil.
/// </summary>
public class OwnerProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string IdentificationNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
