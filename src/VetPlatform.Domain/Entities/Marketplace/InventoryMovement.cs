using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities.Marketplace;

public class InventoryMovement : BaseEntity, IImmutableRecord
{
    public Guid ProductId { get; set; }
    /// <summary>Positive = stock in, negative = stock out.</summary>
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
