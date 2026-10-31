using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities.Marketplace;
namespace VetPlatform.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.HasIndex(x => new { x.BuyerUserId, x.IdempotencyKey }).IsUnique();
        b.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.OrderId);
        b.Ignore(x => x.Total);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
