using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities.Clinical;

/// <summary>"Regente" relationship. Only one active relationship per pet is allowed.</summary>
public class AttendingVetRelationship : BaseEntity
{
    public Guid PetId { get; set; }
    public Guid VetUserId { get; set; }
    public Guid? ClinicId { get; set; }
    public DateTimeOffset EstablishedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public void Revoke(DateTimeOffset now)
    {
        IsActive = false;
        RevokedAt = now;
    }
}
