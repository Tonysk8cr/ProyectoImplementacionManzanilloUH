using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetPlatform.Domain.Entities.Scheduling;
namespace VetPlatform.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> b)
    {
        b.Property(x => x.Status).HasMaxLength(20);
        b.HasIndex(x => x.SlotId).IsUnique().HasFilter("\"Status\" = 'Booked'");
        b.HasIndex(x => x.PetId);
    }
}
