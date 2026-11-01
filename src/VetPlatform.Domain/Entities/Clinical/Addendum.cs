using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities.Clinical;

public class Addendum : BaseEntity, IImmutableRecord
{
    public Guid ConsultationId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
