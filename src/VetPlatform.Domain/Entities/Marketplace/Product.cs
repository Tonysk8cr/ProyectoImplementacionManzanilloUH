using VetPlatform.Domain.Common;
using VetPlatform.Domain.Exceptions;

namespace VetPlatform.Domain.Entities.Marketplace;

public class Product : BaseEntity
{
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Physical stock (sum of inventory movements).</summary>
    public int StockOnHand { get; set; }
    /// <summary>Units reserved by pending/accepted orders.</summary>
    public int ReservedStock { get; set; }
    /// <summary>Optimistic concurrency token, incremented on each stock change.</summary>
    public int Version { get; set; }

    public int AvailableStock => StockOnHand - ReservedStock;

    public void Reserve(int quantity)
    {
        if (quantity <= 0) throw new BusinessRuleViolationException("La cantidad debe ser positiva.");
        if (AvailableStock < quantity)
            throw new BusinessRuleViolationException($"Stock insuficiente para '{Name}'. Disponible: {AvailableStock}, solicitado: {quantity}.");
        ReservedStock += quantity;
        Version++;
    }

    public void ReleaseReservation(int quantity)
    {
        ReservedStock = Math.Max(0, ReservedStock - quantity);
        Version++;
    }

    /// <summary>Converts a reservation into a stock exit (on delivery).</summary>
    public void ConsumeReservation(int quantity)
    {
        ReservedStock = Math.Max(0, ReservedStock - quantity);
        StockOnHand -= quantity;
        Version++;
    }

    public void ApplyMovement(int quantity)
    {
        if (StockOnHand + quantity < ReservedStock)
            throw new BusinessRuleViolationException("El movimiento dejaría el stock por debajo de lo reservado.");
        StockOnHand += quantity;
        Version++;
    }
}
