using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities.Emergency;
namespace VetPlatform.Infrastructure.Data.Configurations;

public class EmergencyAccessConfiguration : IEntityTypeConfiguration<EmergencyAccess>
{
    public void Configure(EntityTypeBuilder<EmergencyAccess> b)
    {
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
    }
}
