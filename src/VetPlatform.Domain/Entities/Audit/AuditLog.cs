using VetPlatform.Domain.Common;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Domain.Entities.Audit;

/// <summary>Append-only audit trail. MUST NOT contain clinical content.</summary>
public class AuditLog : BaseEntity, IImmutableRecord
{
    public Guid ActorUserId { get; set; }
    public AuditAction Action { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public string Result { get; set; } = Results.Allowed;
    public DateTimeOffset Timestamp { get; set; }
    public string? Context { get; set; }

    public static class Results
    {
        public const string Allowed = "Allowed";
        public const string Denied = "Denied";
    }
}
