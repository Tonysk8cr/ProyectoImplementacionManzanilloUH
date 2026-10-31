using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Interfaces.Repositories;

public interface IBaseRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    /// <summary>Soft delete: marks the entity as deleted.</summary>
    Task DeleteAsync(T entity, CancellationToken ct = default);
}
