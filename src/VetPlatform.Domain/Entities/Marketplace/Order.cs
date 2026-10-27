using VetPlatform.Domain.Common;
using VetPlatform.Domain.Enums;
using VetPlatform.Domain.Exceptions;

namespace VetPlatform.Domain.Entities.Marketplace;

public class Order : BaseEntity
{
    public Guid BuyerUserId { get; set; }
    public Guid StoreId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Notes { get; set; }
    /// <summary>Client supplied key; same buyer + key returns the original order (idempotency).</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public decimal Total => Items.Sum(i => i.UnitPrice * i.Quantity);

    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Accepted, OrderStatus.Rejected, OrderStatus.Cancelled },
        [OrderStatus.Accepted] = new[] { OrderStatus.InPreparation, OrderStatus.Cancelled },
        [OrderStatus.InPreparation] = new[] { OrderStatus.Delivered, OrderStatus.Cancelled },
        [OrderStatus.Rejected] = Array.Empty<OrderStatus>(),
        [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>(),
    };

    public bool CanTransitionTo(OrderStatus next) => AllowedTransitions[Status].Contains(next);

    public void TransitionTo(OrderStatus next, DateTimeOffset now)
    {
        if (!CanTransitionTo(next))
            throw new BusinessRuleViolationException($"Transición de pedido inválida: {Status} → {next}.");
        Status = next;
        UpdatedAt = now;
    }
}
