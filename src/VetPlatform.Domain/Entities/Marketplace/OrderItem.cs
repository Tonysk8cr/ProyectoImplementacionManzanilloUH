using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities.Marketplace;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
}
