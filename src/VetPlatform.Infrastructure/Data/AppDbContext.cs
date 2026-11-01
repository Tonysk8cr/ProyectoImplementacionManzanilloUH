using Microsoft.EntityFrameworkCore;
using VetPlatform.Domain.Common;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Entities.Clinical;
using VetPlatform.Domain.Entities.Emergency;
using VetPlatform.Domain.Entities.Identity;
using VetPlatform.Domain.Entities.Marketplace;
using VetPlatform.Domain.Entities.Notifications;
using VetPlatform.Domain.Entities.Organizations;
using VetPlatform.Domain.Entities.Scheduling;
using VetPlatform.Domain.Exceptions;

namespace VetPlatform.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<ClinicMember> ClinicMembers => Set<ClinicMember>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<ClinicalRecord> ClinicalRecords => Set<ClinicalRecord>();
    public DbSet<AttendingVetRelationship> AttendingVetRelationships => Set<AttendingVetRelationship>();
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<Addendum> Addenda => Set<Addendum>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionAddendum> PrescriptionAddenda => Set<PrescriptionAddendum>();
    public DbSet<EmergencyAccess> EmergencyAccesses => Set<EmergencyAccess>();
    public DbSet<EmergencyAttendance> EmergencyAttendances => Set<EmergencyAttendance>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<AvailabilitySlot> AvailabilitySlots => Set<AvailabilitySlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<ClinicMember>().HasIndex(x => new { x.ClinicId, x.UserId });
        modelBuilder.Entity<Clinic>().HasMany(c => c.Members).WithOne().HasForeignKey(m => m.ClinicId);
        modelBuilder.Entity<Store>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<AttendingVetRelationship>().HasIndex(x => x.PetId).IsUnique().HasFilter("\"IsActive\" = TRUE");
        modelBuilder.Entity<EmergencyAccess>().HasIndex(x => new { x.PetId, x.VetUserId, x.Status });
        modelBuilder.Entity<AvailabilitySlot>().Property(x => x.Version).IsConcurrencyToken();
        modelBuilder.Entity<AvailabilitySlot>().HasIndex(x => new { x.VetUserId, x.StartTime });
        modelBuilder.Entity<Notification>().HasIndex(x => new { x.Status, x.CreatedAt });
        modelBuilder.Entity<AuditLog>().HasIndex(x => new { x.ResourceType, x.ResourceId, x.Timestamp });
        modelBuilder.Entity<AuditLog>().Property(x => x.Action).HasConversion<string>().HasMaxLength(30);
        modelBuilder.Entity<UserRoleAssignment>().Property(x => x.Role).HasConversion<string>().HasMaxLength(30);

        // Soft-delete global filter for every entity.
        foreach (var et in modelBuilder.Model.GetEntityTypes().Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
        {
            var p = System.Linq.Expressions.Expression.Parameter(et.ClrType, "e");
            var body = System.Linq.Expressions.Expression.Not(
                System.Linq.Expressions.Expression.Property(p, nameof(BaseEntity.IsDeleted)));
            et.SetQueryFilter(System.Linq.Expressions.Expression.Lambda(body, p));
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Clinical records and audit entries are append-only: corrections must be addenda.
        var violated = ChangeTracker.Entries()
            .Where(e => e.Entity is IImmutableRecord && e.State is EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.GetType().Name).ToList();
        if (violated.Count > 0)
            throw new BusinessRuleViolationException($"Registros inmutables no pueden modificarse: {string.Join(", ", violated)}. Use adendas.");
        return base.SaveChangesAsync(cancellationToken);
    }
}
