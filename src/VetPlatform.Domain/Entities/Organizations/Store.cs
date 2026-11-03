using VetPlatform.Domain.Common;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Domain.Entities.Organizations;

public class Store : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public StoreStatus Status { get; set; } = StoreStatus.Active;
    public Guid OperatorUserId { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
