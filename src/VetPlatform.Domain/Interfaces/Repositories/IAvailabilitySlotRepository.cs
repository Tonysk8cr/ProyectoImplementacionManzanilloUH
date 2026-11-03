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

public interface IAvailabilitySlotRepository : IBaseRepository<AvailabilitySlot>
{
    Task<IReadOnlyList<AvailabilitySlot>> SearchAsync(Guid? clinicId, Guid? vetUserId, DateOnly? date, bool onlyFree, CancellationToken ct = default);
    Task<bool> HasOverlapAsync(Guid vetUserId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default);
}
