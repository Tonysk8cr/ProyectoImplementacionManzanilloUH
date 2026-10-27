using VetPlatform.Domain.Enums;
namespace VetPlatform.Application.Features.Marketplace;

public record OrderItemRequest(Guid ProductId, int Quantity);
public record CreateOrderCommand(Guid BuyerUserId, Guid StoreId, IReadOnlyList<OrderItemRequest> Items, string IdempotencyKey, string? Notes = null);
public record AcceptOrderCommand(Guid OrderId);
public record RejectOrderCommand(Guid OrderId, string? Reason);
public record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus);
