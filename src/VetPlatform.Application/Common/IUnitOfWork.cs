namespace VetPlatform.Application.Common;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    /// <summary>Runs <paramref name="action"/> inside an explicit database transaction (commit on success, rollback on error).</summary>
    /// <summary>Detaches pending (unsaved) added/modified entities after a failed attempt.</summary>
    void DiscardUnsavedChanges();
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
}
