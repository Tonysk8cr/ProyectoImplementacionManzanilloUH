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

public interface IAttendingVetRelationshipRepository : IBaseRepository<AttendingVetRelationship>
{
    Task<AttendingVetRelationship?> GetActiveForPetAsync(Guid petId, CancellationToken ct = default);
    Task<bool> IsActiveAttendingVetAsync(Guid petId, Guid vetUserId, CancellationToken ct = default);
    Task<IReadOnlyList<AttendingVetRelationship>> GetActiveByVetAndClinicAsync(Guid vetUserId, Guid clinicId, CancellationToken ct = default);
}
