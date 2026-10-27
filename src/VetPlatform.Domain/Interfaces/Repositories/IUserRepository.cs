using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Entities.Clinical;
using VetPlatform.Domain.Entities.Emergency;
using VetPlatform.Domain.Entities.Identity;
using VetPlatform.Domain.Entities.Marketplace;
using VetPlatform.Domain.Entities.Notifications;
using VetPlatform.Domain.Entities.Organizations;
using VetPlatform.Domain.Entities.Scheduling;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Domain.Interfaces.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetWithRolesAsync(Guid id, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task AddRoleAsync(UserRoleAssignment assignment, CancellationToken ct = default);
}
