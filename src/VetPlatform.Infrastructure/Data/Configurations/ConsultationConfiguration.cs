using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities.Clinical;
namespace VetPlatform.Infrastructure.Data.Configurations;

public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
{
    public void Configure(EntityTypeBuilder<Consultation> b)
    {
        b.Property(x => x.Notes).IsRequired();
        b.Property(x => x.SourceType).HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasMany(x => x.Addenda).WithOne().HasForeignKey(a => a.ConsultationId);
        b.HasMany(x => x.Prescriptions).WithOne().HasForeignKey(p => p.ConsultationId);
    }
}
