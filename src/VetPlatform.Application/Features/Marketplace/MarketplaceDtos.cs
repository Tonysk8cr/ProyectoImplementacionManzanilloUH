namespace VetPlatform.Application.Features.Marketplace;

public record StoreDto(Guid Id, string Name, string Status, Guid OperatorUserId, string Address);
public record ProductDto(Guid Id, Guid StoreId, string Name, string? Description, decimal Price, bool IsPublished, int AvailableStock);
public record OrderItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, int ReservedQuantity);
public record OrderDto(Guid Id, Guid BuyerUserId, Guid StoreId, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string? Notes, decimal Total, IReadOnlyList<OrderItemDto> Items);
public record InventoryStatusDto(Guid ProductId, int StockOnHand, int ReservedStock, int AvailableStock);
