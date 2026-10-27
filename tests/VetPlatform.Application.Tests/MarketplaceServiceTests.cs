using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Marketplace;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Data;
using Xunit;

namespace VetPlatform.Application.Tests;

public class MarketplaceServiceTests : IDisposable
{
    private readonly TestFixture _f = new();
    public void Dispose() => _f.Dispose();
    private static readonly UserRole[] Op = { UserRole.StoreOperator };
    private static readonly UserRole[] Buyer = { UserRole.PetOwner };

    private async Task<(Guid op, Guid store, Guid product)> SeedStoreAsync(int stock)
    {
        var admin = await _f.SeedUserAsync(UserRole.PlatformAdmin);
        var op = await _f.SeedUserAsync(UserRole.StoreOperator);
        var store = await _f.RunAs<IMarketplaceService, Result<StoreDto>>(admin, new[] { UserRole.PlatformAdmin }, s => s.CreateStoreAsync(new CreateStoreCommand("PetShop", op, "Av 1")));
        var p = await _f.RunAs<IMarketplaceService, Result<ProductDto>>(op, Op, s => s.CreateProductAsync(new CreateProductCommand(store.Value!.Id, "Croquetas", null, 25m)));
        await _f.RunAs<IMarketplaceService, Result<ProductDto>>(op, Op, s => s.PublishProductAsync(new PublishProductCommand(p.Value!.Id)));
        var inv = await _f.RunAs<IMarketplaceService, Result<InventoryStatusDto>>(op, Op, s => s.RegisterInventoryMovementAsync(new RegisterInventoryMovementCommand(p.Value!.Id, stock, "Compra inicial")));
        inv.Success.Should().BeTrue();
        return (op, store.Value!.Id, p.Value!.Id);
    }

    private Task<Result<OrderDto>> OrderAsync(Guid buyer, Guid store, Guid product, int qty, string key) =>
        _f.RunAs<IMarketplaceService, Result<OrderDto>>(buyer, Buyer, s =>
            s.CreateOrderAsync(new CreateOrderCommand(buyer, store, new[] { new OrderItemRequest(product, qty) }, key)));

    private async Task<(int onHand, int reserved)> StockAsync(Guid productId)
    {
        using var scope = _f.NewScope();
        var p = await _f.Get<AppDbContext>(scope).Products.SingleAsync(x => x.Id == productId);
        return (p.StockOnHand, p.ReservedStock);
    }

    [Fact]
    public async Task Order_reserves_stock_atomically()
    {
        var (_, store, product) = await SeedStoreAsync(10);
        var buyer = await _f.SeedUserAsync(UserRole.PetOwner);
        var r = await OrderAsync(buyer, store, product, 4, "k1");
        r.Success.Should().BeTrue(string.Join(",", r.Errors));
        r.Value!.Total.Should().Be(100m);
        (await StockAsync(product)).Should().Be((10, 4));
    }

    [Fact]
    public async Task Order_with_insufficient_stock_on_any_line_reserves_nothing()
    {
        var (op, store, product) = await SeedStoreAsync(5);
        var p2 = await _f.RunAs<IMarketplaceService, Result<ProductDto>>(op, Op, s => s.CreateProductAsync(new CreateProductCommand(store, "Collar", null, 10m)));
        await _f.RunAs<IMarketplaceService, Result<ProductDto>>(op, Op, s => s.PublishProductAsync(new PublishProductCommand(p2.Value!.Id)));
        var buyer = await _f.SeedUserAsync(UserRole.PetOwner);

        var r = await _f.RunAs<IMarketplaceService, Result<OrderDto>>(buyer, Buyer, s => s.CreateOrderAsync(new CreateOrderCommand(buyer, store,
            new[] { new OrderItemRequest(product, 2), new OrderItemRequest(p2.Value!.Id, 1) }, "multi")));
        r.ErrorType.Should().Be(ErrorType.Conflict);
        (await StockAsync(product)).Should().Be((5, 0));
        using var scope = _f.NewScope();
        (await _f.Get<AppDbContext>(scope).Orders.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Competing_orders_cannot_oversell()
    {
        var (_, store, product) = await SeedStoreAsync(5);
        var b1 = await _f.SeedUserAsync(UserRole.PetOwner);
        var b2 = await _f.SeedUserAsync(UserRole.PetOwner);
        (await OrderAsync(b1, store, product, 3, "a")).Success.Should().BeTrue();
        var second = await OrderAsync(b2, store, product, 3, "b");
        second.ErrorType.Should().Be(ErrorType.Conflict);
        (await StockAsync(product)).Should().Be((5, 3));
    }

    [Fact]
    public async Task Stale_concurrent_update_is_detected_by_version_token()
    {
        var (_, _, product) = await SeedStoreAsync(5);
        using var s1 = _f.NewScope();
        using var s2 = _f.NewScope();
        var db1 = _f.Get<AppDbContext>(s1);
        var db2 = _f.Get<AppDbContext>(s2);
        var p1 = await db1.Products.SingleAsync(p => p.Id == product);
        var p2 = await db2.Products.SingleAsync(p => p.Id == product);
        p1.Reserve(3);
        p2.Reserve(3);
        await db1.SaveChangesAsync();
        var act = () => db2.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        (await StockAsync(product)).Should().Be((5, 3));
    }

    [Fact]
    public async Task Same_idempotency_key_returns_same_order_without_double_reservation()
    {
        var (_, store, product) = await SeedStoreAsync(10);
        var buyer = await _f.SeedUserAsync(UserRole.PetOwner);
        var first = await OrderAsync(buyer, store, product, 2, "same-key");
        var retry = await OrderAsync(buyer, store, product, 2, "same-key");
        retry.Value!.Id.Should().Be(first.Value!.Id);
        (await StockAsync(product)).Should().Be((10, 2));
    }

    [Fact]
    public async Task Reject_releases_and_delivery_consumes_reservation()
    {
        var (op, store, product) = await SeedStoreAsync(10);
        var buyer = await _f.SeedUserAsync(UserRole.PetOwner);
        var o1 = await OrderAsync(buyer, store, product, 3, "o1");
        var o2 = await OrderAsync(buyer, store, product, 2, "o2");
        (await _f.RunAs<IMarketplaceService, Result<OrderDto>>(op, Op, s => s.RejectOrderAsync(new RejectOrderCommand(o1.Value!.Id, "Sin reparto")))).Success.Should().BeTrue();
        (await StockAsync(product)).Should().Be((10, 2));

        await _f.RunAs<IMarketplaceService, Result<OrderDto>>(op, Op, s => s.AcceptOrderAsync(new AcceptOrderCommand(o2.Value!.Id)));
        await _f.RunAs<IMarketplaceService, Result<OrderDto>>(op, Op, s => s.UpdateOrderStatusAsync(new UpdateOrderStatusCommand(o2.Value!.Id, OrderStatus.InPreparation)));
        var delivered = await _f.RunAs<IMarketplaceService, Result<OrderDto>>(op, Op, s => s.UpdateOrderStatusAsync(new UpdateOrderStatusCommand(o2.Value!.Id, OrderStatus.Delivered)));
        delivered.Value!.Status.Should().Be("Delivered");
        (await StockAsync(product)).Should().Be((8, 0));
    }
}
