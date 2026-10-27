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

public interface IInventoryMovementRepository : IBaseRepository<InventoryMovement>
{
    Task<IReadOnlyList<InventoryMovement>> GetByProductAsync(Guid productId, CancellationToken ct = default);
}
