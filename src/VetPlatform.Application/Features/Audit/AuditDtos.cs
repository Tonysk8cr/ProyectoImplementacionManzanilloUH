namespace VetPlatform.Application.Features.Audit;

public record AuditLogDto(Guid Id, Guid ActorUserId, string Action, string ResourceType, string ResourceId, Guid? OrganizationId, string Result, DateTimeOffset Timestamp, string? Context);
