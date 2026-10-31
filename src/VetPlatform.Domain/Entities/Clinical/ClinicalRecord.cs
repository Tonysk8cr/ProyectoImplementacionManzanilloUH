using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities.Clinical;

public class ClinicalRecord : BaseEntity
{
    public Guid PetId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Pet? Pet { get; set; }
    public ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
}
