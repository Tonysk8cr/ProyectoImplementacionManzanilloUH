namespace VetPlatform.Application.Features.Clinical;

public record CreatePetCommand(Guid OwnerId, string Name, string Species, string? Breed, DateOnly? BirthDate);
