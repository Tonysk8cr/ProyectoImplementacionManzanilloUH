using VetPlatform.Domain.Common;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Domain.Entities.Identity;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public bool IsSuspended { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<UserRoleAssignment> UserRoles { get; set; } = new List<UserRoleAssignment>();

    public IEnumerable<UserRoleAssignment> ActiveRoles => UserRoles.Where(r => r.RevokedAt == null);

    public bool HasRole(UserRole role, Guid? organizationId = null) =>
        ActiveRoles.Any(r => r.Role == role && (organizationId == null || r.OrganizationId == organizationId));
}
