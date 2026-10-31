using VetPlatform.Domain.Common;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Domain.Entities.Clinical;

public class Pet : BaseEntity
{
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public DateOnly? BirthDate { get; set; }
    public PetStatus Status { get; set; } = PetStatus.Active;
    /// <summary>Opaque public identifier encoded in the QR. Never equal to <see cref="BaseEntity.Id"/>.</summary>
    public string QrCode { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public static string GenerateOpaqueQrCode() => Guid.NewGuid().ToString("N");
}
