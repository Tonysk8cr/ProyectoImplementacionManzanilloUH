namespace VetPlatform.Domain.Common;

/// <summary>Base for all persisted entities. Supports soft delete.</summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
}

/// <summary>Marker for entities that must never be modified after creation (clinical records, audit).</summary>
public interface IImmutableRecord { }
