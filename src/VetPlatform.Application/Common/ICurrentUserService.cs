using VetPlatform.Domain.Enums;

namespace VetPlatform.Application.Common;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    IReadOnlyCollection<UserRole> Roles { get; }
    Guid? OrganizationId { get; }
    IReadOnlyCollection<Guid> OrganizationIds { get; }
    bool IsInRole(UserRole role) => Roles.Contains(role);
}
