using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Features.Marketplace;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.API.Controllers;

[Route("api/orders"), Authorize]
public class OrdersController : ApiControllerBase
{
    private readonly IMarketplaceService _market;
    public OrdersController(IMarketplaceService market) => _market = market;

    /// <summary>IdempotencyKey may come in the body or in the "Idempotency-Key" header.</summary>
    [Authorize(Policy = AuthorizationPolicies.RequirePetOwner), HttpPost]
    public async Task<IActionResult> Create(CreateOrderCommand cmd, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken ct) =>
        ToActionResult(await _market.CreateOrderAsync(cmd with { BuyerUserId = CurrentUserId, IdempotencyKey = string.IsNullOrWhiteSpace(cmd.IdempotencyKey) ? idempotencyKey ?? "" : cmd.IdempotencyKey }, ct), 201);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => ToActionResult(await _market.GetOrderAsync(id, ct));

    [Authorize(Policy = AuthorizationPolicies.RequireStoreOperator), HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct) => ToActionResult(await _market.AcceptOrderAsync(new AcceptOrderCommand(id), ct));

    public record RejectRequest(string? Reason);

    [Authorize(Policy = AuthorizationPolicies.RequireStoreOperator), HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, RejectRequest body, CancellationToken ct) => ToActionResult(await _market.RejectOrderAsync(new RejectOrderCommand(id, body.Reason), ct));

    public record StatusRequest(OrderStatus Status);

    [Authorize(Policy = AuthorizationPolicies.RequireStoreOperator), HttpPost("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, StatusRequest body, CancellationToken ct) =>
        ToActionResult(await _market.UpdateOrderStatusAsync(new UpdateOrderStatusCommand(id, body.Status), ct));
}
