using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities.Marketplace;
namespace VetPlatform.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Price).HasPrecision(12, 2);
        b.Property(x => x.Version).IsConcurrencyToken(); // optimistic locking for stock reservation
        b.Ignore(x => x.AvailableStock);
        b.HasIndex(x => x.StoreId);
    }
}
