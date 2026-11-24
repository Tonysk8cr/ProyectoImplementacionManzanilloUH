using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities.Identity;

namespace VetPlatform.Infrastructure.Data.Configurations;

public class OwnerProfileConfiguration : IEntityTypeConfiguration<OwnerProfile>
{
    public void Configure(EntityTypeBuilder<OwnerProfile> b)
    {
        b.Property(x => x.IdentificationNumber).HasMaxLength(20).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        b.Property(x => x.Address).HasMaxLength(300);

        // Un perfil activo por usuario y una identificación activa por perfil.
        // El filtro permite volver a registrarse tras una eliminación lógica (IsDeleted).
        b.HasIndex(x => x.UserId).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => x.IdentificationNumber).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
