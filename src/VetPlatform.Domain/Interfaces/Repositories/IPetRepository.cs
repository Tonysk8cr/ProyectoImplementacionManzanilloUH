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

public interface IPetRepository : IBaseRepository<Pet>
{
    Task<Pet?> GetByQrCodeAsync(string qrCode, CancellationToken ct = default);
    Task<IReadOnlyList<Pet>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
}
