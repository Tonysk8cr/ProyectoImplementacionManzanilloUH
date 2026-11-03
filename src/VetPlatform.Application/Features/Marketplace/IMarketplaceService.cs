using VetPlatform.Application.Common;
namespace VetPlatform.Application.Features.Marketplace;

public interface IMarketplaceService
{
    Task<Result<StoreDto>> CreateStoreAsync(CreateStoreCommand cmd, CancellationToken ct = default);
    Task<Result<ProductDto>> CreateProductAsync(CreateProductCommand cmd, CancellationToken ct = default);
    Task<Result<ProductDto>> UpdateProductAsync(UpdateProductCommand cmd, CancellationToken ct = default);
    Task<Result<ProductDto>> PublishProductAsync(PublishProductCommand cmd, CancellationToken ct = default);
    Task<Result<ProductDto>> UnpublishProductAsync(UnpublishProductCommand cmd, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProductDto>>> GetProductsAsync(Guid storeId, CancellationToken ct = default);
    Task<Result<InventoryStatusDto>> RegisterInventoryMovementAsync(RegisterInventoryMovementCommand cmd, CancellationToken ct = default);
    Task<Result<OrderDto>> CreateOrderAsync(CreateOrderCommand cmd, CancellationToken ct = default);
    Task<Result<OrderDto>> GetOrderAsync(Guid id, CancellationToken ct = default);
    Task<Result<OrderDto>> AcceptOrderAsync(AcceptOrderCommand cmd, CancellationToken ct = default);
    Task<Result<OrderDto>> RejectOrderAsync(RejectOrderCommand cmd, CancellationToken ct = default);
    Task<Result<OrderDto>> UpdateOrderStatusAsync(UpdateOrderStatusCommand cmd, CancellationToken ct = default);
}
