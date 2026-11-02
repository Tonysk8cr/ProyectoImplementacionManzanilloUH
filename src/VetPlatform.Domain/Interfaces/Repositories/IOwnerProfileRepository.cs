using VetPlatform.Domain.Entities.Identity;

namespace VetPlatform.Domain.Interfaces.Repositories;

public interface IOwnerProfileRepository : IBaseRepository<OwnerProfile>
{
    Task<OwnerProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Indica si otro perfil activo ya usa esa identificación (se excluye el perfil indicado, útil al editar).</summary>
    Task<bool> IdentificationExistsAsync(string identificationNumber, Guid? excludingProfileId = null, CancellationToken ct = default);
}
