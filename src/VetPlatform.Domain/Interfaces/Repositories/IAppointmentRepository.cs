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

public interface IAppointmentRepository : IBaseRepository<Appointment>
{
    Task<IReadOnlyList<Appointment>> GetByPetAsync(Guid petId, CancellationToken ct = default);
    Task<bool> ExistsActiveForSlotAsync(Guid slotId, CancellationToken ct = default);
}
