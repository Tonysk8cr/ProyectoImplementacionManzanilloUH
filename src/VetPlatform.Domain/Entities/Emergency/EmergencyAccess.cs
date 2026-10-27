using VetPlatform.Domain.Common;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Domain.Entities.Emergency;

/// <summary>Temporary, justified, audited access of an emergency vet to a pet's clinical record.</summary>
public class EmergencyAccess : BaseEntity
{
    public Guid PetId { get; set; }
    public Guid VetUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset GrantedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public EmergencyAccessStatus Status { get; set; } = EmergencyAccessStatus.Active;
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Evaluates validity at <paramref name="now"/>; transitions to Expired when the window has passed.</summary>
    public bool IsValidAt(DateTimeOffset now)
    {
        if (Status == EmergencyAccessStatus.Active && now >= ExpiresAt)
            Status = EmergencyAccessStatus.Expired;
        return Status == EmergencyAccessStatus.Active;
    }

    public void Revoke(DateTimeOffset now)
    {
        Status = EmergencyAccessStatus.Revoked;
        RevokedAt = now;
    }
}
