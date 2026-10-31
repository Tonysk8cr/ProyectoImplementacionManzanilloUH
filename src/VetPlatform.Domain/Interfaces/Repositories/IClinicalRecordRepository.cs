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

public interface IClinicalRecordRepository : IBaseRepository<ClinicalRecord>
{
    Task<ClinicalRecord?> GetByPetIdAsync(Guid petId, CancellationToken ct = default);
    /// <summary>Loads the record with consultations, addenda and prescriptions.</summary>
    Task<ClinicalRecord?> GetFullHistoryAsync(Guid petId, CancellationToken ct = default);
}
