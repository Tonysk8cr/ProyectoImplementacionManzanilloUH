namespace VetPlatform.Application.Features.Marketplace;

public record RegisterInventoryMovementCommand(Guid ProductId, int Quantity, string Reason);
