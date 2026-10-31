using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities.Clinical;
namespace VetPlatform.Infrastructure.Data.Configurations;

public class ClinicalRecordConfiguration : IEntityTypeConfiguration<ClinicalRecord>
{
    public void Configure(EntityTypeBuilder<ClinicalRecord> b)
    {
        b.HasIndex(x => x.PetId).IsUnique();
        b.HasOne(x => x.Pet).WithMany().HasForeignKey(x => x.PetId);
        b.HasMany(x => x.Consultations).WithOne().HasForeignKey(c => c.ClinicalRecordId);
    }
}
