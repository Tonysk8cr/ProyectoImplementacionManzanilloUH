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
    Task<bool> ExistsActiveForSlotAsync(Guid slotId, CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentDetails>> SearchDetailsAsync(
        Guid? appointmentId = null,
        Guid? petId = null,
        Guid? ownerId = null,
        Guid? vetUserId = null,
        Guid? clinicId = null,
        IReadOnlyCollection<Guid>? clinicIds = null,
        CancellationToken ct = default);
}
