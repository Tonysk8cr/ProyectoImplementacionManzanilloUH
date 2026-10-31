using VetPlatform.Domain.Common;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Domain.Entities.Identity;

public class UserRoleAssignment : BaseEntity
{
    public Guid UserId { get; set; }
    public UserRole Role { get; set; }
    public Guid? OrganizationId { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public User? User { get; set; }
}
