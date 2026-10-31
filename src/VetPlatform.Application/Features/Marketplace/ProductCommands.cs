namespace VetPlatform.Application.Features.Marketplace;

public record CreateStoreCommand(string Name, Guid OperatorUserId, string Address);
public record CreateProductCommand(Guid StoreId, string Name, string? Description, decimal Price);
public record UpdateProductCommand(Guid ProductId, string Name, string? Description, decimal Price);
public record PublishProductCommand(Guid ProductId);
public record UnpublishProductCommand(Guid ProductId);
