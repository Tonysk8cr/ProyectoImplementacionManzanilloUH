using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities.Clinical;
namespace VetPlatform.Infrastructure.Data.Configurations;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> b)
    {
        b.Property(x => x.Medication).HasMaxLength(200).IsRequired();
        b.Property(x => x.Dosage).HasMaxLength(100).IsRequired();
        b.Property(x => x.Route).HasMaxLength(50).IsRequired();
        b.Property(x => x.Frequency).HasMaxLength(100).IsRequired();
        b.HasMany(x => x.Addenda).WithOne().HasForeignKey(a => a.PrescriptionId);
    }
}
