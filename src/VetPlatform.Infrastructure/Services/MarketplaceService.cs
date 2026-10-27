using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Application.Features.Marketplace;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Entities.Marketplace;
using VetPlatform.Domain.Entities.Organizations;
using VetPlatform.Domain.Enums;
using VetPlatform.Domain.Exceptions;
using VetPlatform.Domain.Interfaces.Repositories;

namespace VetPlatform.Infrastructure.Services;

public class MarketplaceService : IMarketplaceService
{
    private const int MaxConcurrencyRetries = 3;
    private readonly IStoreRepository _stores;
    private readonly IProductRepository _products;
    private readonly IInventoryMovementRepository _movements;
    private readonly IOrderRepository _orders;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;
    private readonly IValidator<CreateOrderCommand> _orderValidator;

    public MarketplaceService(IStoreRepository stores, IProductRepository products, IInventoryMovementRepository movements, IOrderRepository orders,
        INotificationService notifications, IAuditService audit, ICurrentUserService user, IUnitOfWork uow, TimeProvider time, IValidator<CreateOrderCommand> orderValidator)
    {
        _stores = stores; _products = products; _movements = movements; _orders = orders; _notifications = notifications;
        _audit = audit; _user = user; _uow = uow; _time = time; _orderValidator = orderValidator;
    }

    private Guid Actor => _user.UserId ?? Guid.Empty;

    private async Task<Store?> GetOperatedStoreAsync(Guid storeId, CancellationToken ct)
    {
        var store = await _stores.GetByIdAsync(storeId, ct);
        return store != null && store.OperatorUserId == Actor && _user.IsInRole(UserRole.StoreOperator) ? store : null;
    }

    public async Task<Result<StoreDto>> CreateStoreAsync(CreateStoreCommand cmd, CancellationToken ct = default)
    {
        if (!_user.IsInRole(UserRole.PlatformAdmin)) return Result<StoreDto>.Forbidden();
        if (string.IsNullOrWhiteSpace(cmd.Name)) return Result<StoreDto>.Invalid("El nombre es obligatorio.");
        var store = new Store { Name = cmd.Name.Trim(), OperatorUserId = cmd.OperatorUserId, Address = cmd.Address, CreatedAt = _time.GetUtcNow() };
        await _stores.AddAsync(store, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(Actor, AuditAction.Create, "Store", store.Id.ToString(), store.Id, AuditLog.Results.Allowed, null, ct);
        return Result<StoreDto>.Ok(store.ToDto());
    }

    public async Task<Result<ProductDto>> CreateProductAsync(CreateProductCommand cmd, CancellationToken ct = default)
    {
        var store = await GetOperatedStoreAsync(cmd.StoreId, ct);
        if (store == null) return Result<ProductDto>.Forbidden("No opera esta tienda.");
        if (string.IsNullOrWhiteSpace(cmd.Name) || cmd.Price < 0) return Result<ProductDto>.Invalid("Nombre y precio válidos son obligatorios.");
        var now = _time.GetUtcNow();
        var p = new Product { StoreId = store.Id, Name = cmd.Name.Trim(), Description = cmd.Description, Price = cmd.Price, CreatedAt = now, UpdatedAt = now };
        await _products.AddAsync(p, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<ProductDto>.Ok(p.ToDto());
    }

    private async Task<Result<ProductDto>> MutateProductAsync(Guid productId, Action<Product> mutate, CancellationToken ct)
    {
        var p = await _products.GetByIdAsync(productId, ct);
        if (p == null) return Result<ProductDto>.NotFound("Producto no encontrado.");
        if (await GetOperatedStoreAsync(p.StoreId, ct) == null) return Result<ProductDto>.Forbidden("No opera esta tienda.");
        mutate(p);
        p.UpdatedAt = _time.GetUtcNow();
        await _uow.SaveChangesAsync(ct);
        return Result<ProductDto>.Ok(p.ToDto());
    }

    public Task<Result<ProductDto>> UpdateProductAsync(UpdateProductCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Name) || cmd.Price < 0) return Task.FromResult(Result<ProductDto>.Invalid("Nombre y precio válidos son obligatorios."));
        return MutateProductAsync(cmd.ProductId, p => { p.Name = cmd.Name.Trim(); p.Description = cmd.Description; p.Price = cmd.Price; }, ct);
    }

    public Task<Result<ProductDto>> PublishProductAsync(PublishProductCommand cmd, CancellationToken ct = default) =>
        MutateProductAsync(cmd.ProductId, p => p.IsPublished = true, ct);

    public Task<Result<ProductDto>> UnpublishProductAsync(UnpublishProductCommand cmd, CancellationToken ct = default) =>
        MutateProductAsync(cmd.ProductId, p => p.IsPublished = false, ct);

    public async Task<Result<IReadOnlyList<ProductDto>>> GetProductsAsync(Guid storeId, CancellationToken ct = default)
    {
        var isOperator = await GetOperatedStoreAsync(storeId, ct) != null;
        var list = await _products.GetByStoreAsync(storeId, publishedOnly: !isOperator, ct);
        return Result<IReadOnlyList<ProductDto>>.Ok(list.Select(p => p.ToDto()).ToList());
    }

    public async Task<Result<InventoryStatusDto>> RegisterInventoryMovementAsync(RegisterInventoryMovementCommand cmd, CancellationToken ct = default)
    {
        if (cmd.Quantity == 0 || string.IsNullOrWhiteSpace(cmd.Reason)) return Result<InventoryStatusDto>.Invalid("Cantidad distinta de cero y motivo son obligatorios.");
        var p = await _products.GetByIdAsync(cmd.ProductId, ct);
        if (p == null) return Result<InventoryStatusDto>.NotFound("Producto no encontrado.");
        if (await GetOperatedStoreAsync(p.StoreId, ct) == null) return Result<InventoryStatusDto>.Forbidden("No opera esta tienda.");
        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                p.ApplyMovement(cmd.Quantity);
                await _movements.AddAsync(new InventoryMovement { ProductId = p.Id, Quantity = cmd.Quantity, Reason = cmd.Reason, UserId = Actor, CreatedAt = _time.GetUtcNow() }, ct);
                await _uow.SaveChangesAsync(ct);
                return true;
            }, ct);
        }
        catch (BusinessRuleViolationException ex) { return Result<InventoryStatusDto>.Conflict(ex.Message); }
        catch (DbUpdateConcurrencyException) { return Result<InventoryStatusDto>.Conflict("El inventario cambió; reintente."); }
        return Result<InventoryStatusDto>.Ok(new InventoryStatusDto(p.Id, p.StockOnHand, p.ReservedStock, p.AvailableStock));
    }

    public async Task<Result<OrderDto>> CreateOrderAsync(CreateOrderCommand cmd, CancellationToken ct = default)
    {
        if (cmd.BuyerUserId != Actor || !_user.IsInRole(UserRole.PetOwner)) return Result<OrderDto>.Forbidden();
        var v = await _orderValidator.ValidateAsync(cmd, ct);
        if (!v.IsValid) return Result<OrderDto>.Invalid(v.Errors.Select(e => e.ErrorMessage).ToArray());

        // Idempotency: same buyer + key returns the original order without reserving again.
        var existing = await _orders.GetByIdempotencyKeyAsync(Actor, cmd.IdempotencyKey, ct);
        if (existing != null) return Result<OrderDto>.Ok(existing.ToDto());

        var store = await _stores.GetByIdAsync(cmd.StoreId, ct);
        if (store == null || store.Status != StoreStatus.Active) return Result<OrderDto>.Invalid("La tienda no está disponible.");
        var lines = cmd.Items.GroupBy(i => i.ProductId).Select(g => (ProductId: g.Key, Quantity: g.Sum(x => x.Quantity))).ToList();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var order = await _uow.ExecuteInTransactionAsync(async () =>
                {
                    var products = (await _products.GetByIdsAsync(lines.Select(l => l.ProductId), ct)).ToDictionary(p => p.Id);
                    var now = _time.GetUtcNow();
                    var o = new Order { BuyerUserId = Actor, StoreId = store.Id, Status = OrderStatus.Pending, CreatedAt = now, UpdatedAt = now,
                        Notes = cmd.Notes, IdempotencyKey = cmd.IdempotencyKey };
                    foreach (var (productId, qty) in lines)
                    {
                        if (!products.TryGetValue(productId, out var p) || p.StoreId != store.Id || !p.IsPublished)
                            throw new BusinessRuleViolationException($"Producto {productId} no disponible en esta tienda.");
                        p.Reserve(qty); // all-or-nothing: any failure rolls back the whole transaction
                        o.Items.Add(new OrderItem { OrderId = o.Id, ProductId = p.Id, ProductName = p.Name, UnitPrice = p.Price, Quantity = qty, ReservedQuantity = qty });
                    }
                    await _orders.AddAsync(o, ct);
                    await _notifications.EnqueueAsync(store.OperatorUserId, "Nuevo pedido", $"Pedido {o.Id} pendiente de aceptación.", o.Id, "Order", ct);
                    await _uow.SaveChangesAsync(ct); // Version tokens detect concurrent reservations
                    return o;
                }, ct);
                await _audit.LogAsync(Actor, AuditAction.Create, "Order", order.Id.ToString(), store.Id, AuditLog.Results.Allowed, null, ct);
                return Result<OrderDto>.Ok(order.ToDto());
            }
            catch (BusinessRuleViolationException ex)
            {
                await DiscardPendingAsync(lines.Select(l => l.ProductId), ct);
                return Result<OrderDto>.Conflict(ex.Message);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                await DiscardPendingAsync(lines.Select(l => l.ProductId), ct); // reload fresh stock and retry
            }
            catch (DbUpdateConcurrencyException)
            {
                await DiscardPendingAsync(lines.Select(l => l.ProductId), ct);
                return Result<OrderDto>.Conflict("Alta concurrencia sobre el stock; reintente.");
            }
            catch (DbUpdateException)
            {
                // Unique (buyer, idempotency key) violated by a concurrent identical request.
                var dup = await _orders.GetByIdempotencyKeyAsync(Actor, cmd.IdempotencyKey, ct);
                if (dup != null) return Result<OrderDto>.Ok(dup.ToDto());
                throw;
            }
        }
    }

    private async Task DiscardPendingAsync(IEnumerable<Guid> productIds, CancellationToken ct)
    {
        foreach (var p in await _products.GetByIdsAsync(productIds, ct)) await _products.ReloadAsync(p, ct);
        _uow.DiscardUnsavedChanges();
    }

    public async Task<Result<OrderDto>> GetOrderAsync(Guid id, CancellationToken ct = default)
    {
        var o = await _orders.GetWithItemsAsync(id, ct);
        if (o == null) return Result<OrderDto>.NotFound("Pedido no encontrado.");
        if (o.BuyerUserId != Actor && await GetOperatedStoreAsync(o.StoreId, ct) == null) return Result<OrderDto>.Forbidden();
        return Result<OrderDto>.Ok(o.ToDto());
    }

    public Task<Result<OrderDto>> AcceptOrderAsync(AcceptOrderCommand cmd, CancellationToken ct = default) =>
        TransitionAsync(cmd.OrderId, OrderStatus.Accepted, null, ct);

    public Task<Result<OrderDto>> RejectOrderAsync(RejectOrderCommand cmd, CancellationToken ct = default) =>
        TransitionAsync(cmd.OrderId, OrderStatus.Rejected, cmd.Reason, ct);

    public Task<Result<OrderDto>> UpdateOrderStatusAsync(UpdateOrderStatusCommand cmd, CancellationToken ct = default) =>
        TransitionAsync(cmd.OrderId, cmd.NewStatus, null, ct);

    private async Task<Result<OrderDto>> TransitionAsync(Guid orderId, OrderStatus next, string? reason, CancellationToken ct)
    {
        var o = await _orders.GetWithItemsAsync(orderId, ct);
        if (o == null) return Result<OrderDto>.NotFound("Pedido no encontrado.");
        if (await GetOperatedStoreAsync(o.StoreId, ct) == null) return Result<OrderDto>.Forbidden("No opera esta tienda.");
        if (!o.CanTransitionTo(next)) return Result<OrderDto>.Conflict($"Transición inválida: {o.Status} → {next}.");
        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var products = (await _products.GetByIdsAsync(o.Items.Select(i => i.ProductId), ct)).ToDictionary(p => p.Id);
                var now = _time.GetUtcNow();
                foreach (var item in o.Items.Where(i => products.ContainsKey(i.ProductId)))
                {
                    var p = products[item.ProductId];
                    if (next is OrderStatus.Rejected or OrderStatus.Cancelled) p.ReleaseReservation(item.ReservedQuantity);
                    else if (next == OrderStatus.Delivered)
                    {
                        p.ConsumeReservation(item.ReservedQuantity);
                        await _movements.AddAsync(new InventoryMovement { ProductId = p.Id, Quantity = -item.ReservedQuantity, Reason = $"Pedido {o.Id} entregado", UserId = Actor, CreatedAt = now }, ct);
                    }
                    if (next is OrderStatus.Rejected or OrderStatus.Cancelled or OrderStatus.Delivered) item.ReservedQuantity = 0;
                }
                o.TransitionTo(next, now);
                if (reason != null) o.Notes = string.IsNullOrEmpty(o.Notes) ? $"Motivo: {reason}" : $"{o.Notes}\nMotivo: {reason}";
                await _notifications.EnqueueAsync(o.BuyerUserId, "Actualización de pedido", $"Su pedido {o.Id} está ahora en estado {next}.", o.Id, "Order", ct);
                await _uow.SaveChangesAsync(ct);
                return true;
            }, ct);
        }
        catch (DbUpdateConcurrencyException) { return Result<OrderDto>.Conflict("El pedido o stock cambió; reintente."); }
        await _audit.LogAsync(Actor, AuditAction.Update, "Order", o.Id.ToString(), o.StoreId, AuditLog.Results.Allowed, $"status={next}", ct);
        return Result<OrderDto>.Ok(o.ToDto());
    }
}
