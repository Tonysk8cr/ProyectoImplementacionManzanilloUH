using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Features.Marketplace;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.API.Controllers;

[Authorize]
public class ProductsController : ApiControllerBase
{
    private readonly IMarketplaceService _market;
    public ProductsController(IMarketplaceService market) => _market = market;

    [Authorize(Policy = AuthorizationPolicies.RequirePlatformAdmin), HttpPost("api/stores")]
    public async Task<IActionResult> CreateStore(CreateStoreCommand cmd, CancellationToken ct) => ToActionResult(await _market.CreateStoreAsync(cmd, ct), 201);

    [Authorize(Policy = AuthorizationPolicies.RequireStoreOperator), HttpPost("api/products")]
    public async Task<IActionResult> Create(CreateProductCommand cmd, CancellationToken ct) => ToActionResult(await _market.CreateProductAsync(cmd, ct), 201);

    [HttpGet("api/products")]
    public async Task<IActionResult> List([FromQuery] Guid storeId, CancellationToken ct) => ToActionResult(await _market.GetProductsAsync(storeId, ct));

    public record UpdateProductRequest(string Name, string? Description, decimal Price);

    [Authorize(Policy = AuthorizationPolicies.RequireStoreOperator), HttpPut("api/products/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest body, CancellationToken ct) =>
        ToActionResult(await _market.UpdateProductAsync(new UpdateProductCommand(id, body.Name, body.Description, body.Price), ct));

    [Authorize(Policy = AuthorizationPolicies.RequireStoreOperator), HttpPost("api/products/{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct) => ToActionResult(await _market.PublishProductAsync(new PublishProductCommand(id), ct));

    [Authorize(Policy = AuthorizationPolicies.RequireStoreOperator), HttpPost("api/products/{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct) => ToActionResult(await _market.UnpublishProductAsync(new UnpublishProductCommand(id), ct));

    [Authorize(Policy = AuthorizationPolicies.RequireStoreOperator), HttpPost("api/inventory/movements")]
    public async Task<IActionResult> Movement(RegisterInventoryMovementCommand cmd, CancellationToken ct) => ToActionResult(await _market.RegisterInventoryMovementAsync(cmd, ct), 201);
}
