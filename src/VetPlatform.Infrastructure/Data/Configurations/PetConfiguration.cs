using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities.Clinical;
namespace VetPlatform.Infrastructure.Data.Configurations;

public class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> b)
    {
        b.HasIndex(x => x.QrCode).IsUnique();
        b.HasIndex(x => x.OwnerId);
        b.Property(x => x.QrCode).HasMaxLength(64).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Species).HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
    }
}
