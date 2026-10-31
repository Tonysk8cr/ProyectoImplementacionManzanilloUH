using VetPlatform.Domain.Common;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Domain.Entities.Organizations;

public class Clinic : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ClinicStatus Status { get; set; } = ClinicStatus.Active;
    public Guid RepresentativeUserId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ClinicMember> Members { get; set; } = new List<ClinicMember>();
}
