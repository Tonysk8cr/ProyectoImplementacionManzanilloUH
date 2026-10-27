using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities.Identity;
namespace VetPlatform.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.PasswordHash).IsRequired();
        b.HasMany(x => x.UserRoles).WithOne(r => r.User).HasForeignKey(r => r.UserId);
        b.Ignore(x => x.ActiveRoles);
    }
}
