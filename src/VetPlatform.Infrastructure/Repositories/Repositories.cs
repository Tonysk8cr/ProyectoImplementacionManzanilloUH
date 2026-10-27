using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common;
using VetPlatform.Domain.Common;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Entities.Clinical;
using VetPlatform.Domain.Entities.Emergency;
using VetPlatform.Domain.Entities.Identity;
using VetPlatform.Domain.Entities.Marketplace;
using VetPlatform.Domain.Entities.Notifications;
using VetPlatform.Domain.Entities.Organizations;
using VetPlatform.Domain.Entities.Scheduling;
using VetPlatform.Domain.Enums;
using VetPlatform.Domain.Interfaces.Repositories;
using VetPlatform.Infrastructure.Data;

namespace VetPlatform.Infrastructure.Repositories;

public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Db;
    protected DbSet<T> Set => Db.Set<T>();
    public BaseRepository(AppDbContext db) => Db = db;

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) => Set.FirstOrDefaultAsync(e => e.Id == id, ct);
    public virtual async Task AddAsync(T entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);
    public virtual Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        if (Db.Entry(entity).State == EntityState.Detached) Set.Update(entity);
        return Task.CompletedTask;
    }
    public virtual Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        entity.IsDeleted = true;
        return UpdateAsync(entity, ct);
    }
}

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Set.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);
    public Task<User?> GetWithRolesAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id, ct);
    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        Set.IgnoreQueryFilters().AnyAsync(u => u.Email == email.ToLower(), ct);
    public async Task AddRoleAsync(UserRoleAssignment a, CancellationToken ct = default) => await Db.UserRoleAssignments.AddAsync(a, ct);
}

public class ClinicRepository : BaseRepository<Clinic>, IClinicRepository
{
    public ClinicRepository(AppDbContext db) : base(db) { }
    public Task<Clinic?> GetWithMembersAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == id, ct);
    public Task<ClinicMember?> GetActiveMemberAsync(Guid clinicId, Guid userId, CancellationToken ct = default) =>
        Db.ClinicMembers.FirstOrDefaultAsync(m => m.ClinicId == clinicId && m.UserId == userId && m.LeftAt == null, ct);
    public async Task AddMemberAsync(ClinicMember m, CancellationToken ct = default) => await Db.ClinicMembers.AddAsync(m, ct);
}

public class StoreRepository : BaseRepository<Store>, IStoreRepository
{
    public StoreRepository(AppDbContext db) : base(db) { }
    public async Task<IReadOnlyList<Store>> GetByOperatorAsync(Guid op, CancellationToken ct = default) =>
        await Set.Where(s => s.OperatorUserId == op).ToListAsync(ct);
}

public class PetRepository : BaseRepository<Pet>, IPetRepository
{
    public PetRepository(AppDbContext db) : base(db) { }
    public Task<Pet?> GetByQrCodeAsync(string qr, CancellationToken ct = default) => Set.FirstOrDefaultAsync(p => p.QrCode == qr, ct);
    public async Task<IReadOnlyList<Pet>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        await Set.Where(p => p.OwnerId == ownerId).ToListAsync(ct);
}

public class ClinicalRecordRepository : BaseRepository<ClinicalRecord>, IClinicalRecordRepository
{
    public ClinicalRecordRepository(AppDbContext db) : base(db) { }
    public Task<ClinicalRecord?> GetByPetIdAsync(Guid petId, CancellationToken ct = default) => Set.FirstOrDefaultAsync(r => r.PetId == petId, ct);
    public Task<ClinicalRecord?> GetFullHistoryAsync(Guid petId, CancellationToken ct = default) =>
        Set.Include(r => r.Consultations).ThenInclude(c => c.Addenda)
           .Include(r => r.Consultations).ThenInclude(c => c.Prescriptions).ThenInclude(p => p.Addenda)
           .AsSplitQuery().FirstOrDefaultAsync(r => r.PetId == petId, ct);
}

public class ConsultationRepository : BaseRepository<Consultation>, IConsultationRepository
{
    public ConsultationRepository(AppDbContext db) : base(db) { }
    public Task<Consultation?> GetWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(c => c.Addenda).Include(c => c.Prescriptions).ThenInclude(p => p.Addenda).AsSplitQuery().FirstOrDefaultAsync(c => c.Id == id, ct);
    public async Task AddAddendumAsync(Addendum a, CancellationToken ct = default) => await Db.Addenda.AddAsync(a, ct);
}

public class PrescriptionRepository : BaseRepository<Prescription>, IPrescriptionRepository
{
    public PrescriptionRepository(AppDbContext db) : base(db) { }
    public Task<Prescription?> GetWithAddendaAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(p => p.Addenda).FirstOrDefaultAsync(p => p.Id == id, ct);
    public async Task AddAddendumAsync(PrescriptionAddendum a, CancellationToken ct = default) => await Db.PrescriptionAddenda.AddAsync(a, ct);
}

public class EmergencyAccessRepository : BaseRepository<EmergencyAccess>, IEmergencyAccessRepository
{
    public EmergencyAccessRepository(AppDbContext db) : base(db) { }
    public async Task<EmergencyAccess?> GetLatestActiveAsync(Guid petId, Guid vetUserId, CancellationToken ct = default)
    {
        // Few active rows per (pet, vet): order client-side to stay provider-agnostic.
        var active = await Set.Where(a => a.PetId == petId && a.VetUserId == vetUserId && a.Status == EmergencyAccessStatus.Active).ToListAsync(ct);
        return active.OrderByDescending(a => a.GrantedAt).FirstOrDefault();
    }
}

public class EmergencyAttendanceRepository : BaseRepository<EmergencyAttendance>, IEmergencyAttendanceRepository
{
    public EmergencyAttendanceRepository(AppDbContext db) : base(db) { }
    public async Task<IReadOnlyList<EmergencyAttendance>> GetByClinicalRecordAsync(Guid id, CancellationToken ct = default) =>
        await Set.Where(a => a.ClinicalRecordId == id).ToListAsync(ct);
}

public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext db) : base(db) { }
    public async Task<IReadOnlyList<Product>> GetByStoreAsync(Guid storeId, bool publishedOnly, CancellationToken ct = default) =>
        await Set.Where(p => p.StoreId == storeId && (!publishedOnly || p.IsPublished)).OrderBy(p => p.Name).ToListAsync(ct);
    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var list = ids.ToList();
        return await Set.Where(p => list.Contains(p.Id)).ToListAsync(ct);
    }
    public Task ReloadAsync(Product product, CancellationToken ct = default) => Db.Entry(product).ReloadAsync(ct);
}

public class InventoryMovementRepository : BaseRepository<InventoryMovement>, IInventoryMovementRepository
{
    public InventoryMovementRepository(AppDbContext db) : base(db) { }
    public async Task<IReadOnlyList<InventoryMovement>> GetByProductAsync(Guid productId, CancellationToken ct = default) =>
        await Set.Where(m => m.ProductId == productId).OrderBy(m => m.CreatedAt).ToListAsync(ct);
}

public class OrderRepository : BaseRepository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext db) : base(db) { }
    public Task<Order?> GetWithItemsAsync(Guid id, CancellationToken ct = default) => Set.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
    public Task<Order?> GetByIdempotencyKeyAsync(Guid buyer, string key, CancellationToken ct = default) =>
        Set.Include(o => o.Items).FirstOrDefaultAsync(o => o.BuyerUserId == buyer && o.IdempotencyKey == key, ct);
}

public class AvailabilitySlotRepository : BaseRepository<AvailabilitySlot>, IAvailabilitySlotRepository
{
    public AvailabilitySlotRepository(AppDbContext db) : base(db) { }
    public async Task<IReadOnlyList<AvailabilitySlot>> SearchAsync(Guid? clinicId, Guid? vetUserId, DateOnly? date, bool onlyFree, CancellationToken ct = default)
    {
        var q = Set.AsQueryable();
        if (clinicId.HasValue) q = q.Where(s => s.ClinicId == clinicId);
        if (vetUserId.HasValue) q = q.Where(s => s.VetUserId == vetUserId);
        if (onlyFree) q = q.Where(s => !s.IsBooked);
        var list = await q.ToListAsync(ct);
        if (date.HasValue) list = list.Where(s => DateOnly.FromDateTime(s.StartTime.UtcDateTime) == date).ToList();
        return list.OrderBy(s => s.StartTime).ToList();
    }
    public async Task<bool> HasOverlapAsync(Guid vetUserId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default)
    {
        var slots = await Set.Where(s => s.VetUserId == vetUserId).ToListAsync(ct);
        return slots.Any(s => s.StartTime < end && start < s.EndTime);
    }
}

public class AppointmentRepository : BaseRepository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(AppDbContext db) : base(db) { }
    public async Task<IReadOnlyList<Appointment>> GetByPetAsync(Guid petId, CancellationToken ct = default) =>
        await Set.Where(a => a.PetId == petId).ToListAsync(ct);
    public Task<bool> ExistsActiveForSlotAsync(Guid slotId, CancellationToken ct = default) =>
        Set.AnyAsync(a => a.SlotId == slotId && a.Status == Appointment.Statuses.Booked, ct);
}

public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext db) : base(db) { }
    public async Task<IReadOnlyList<Notification>> GetPendingAsync(int maxRetries, int batchSize, CancellationToken ct = default) =>
        await Set.Where(n => n.Status != NotificationStatus.Sent && n.RetryCount < maxRetries).OrderBy(n => n.Id).Take(batchSize).ToListAsync(ct);
    public async Task<IReadOnlyList<Notification>> GetByRecipientAsync(Guid r, CancellationToken ct = default) =>
        await Set.Where(n => n.RecipientUserId == r).ToListAsync(ct);
}

public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AppDbContext db) : base(db) { }
    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(string? resourceType, string? resourceId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct = default)
    {
        var q = Set.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(resourceType)) q = q.Where(a => a.ResourceType == resourceType);
        if (!string.IsNullOrWhiteSpace(resourceId)) q = q.Where(a => a.ResourceId == resourceId);
        if (from.HasValue) q = q.Where(a => a.Timestamp >= from);
        if (to.HasValue) q = q.Where(a => a.Timestamp <= to);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.Timestamp).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }
}

public class AttendingVetRelationshipRepository : BaseRepository<AttendingVetRelationship>, IAttendingVetRelationshipRepository
{
    public AttendingVetRelationshipRepository(AppDbContext db) : base(db) { }
    public Task<AttendingVetRelationship?> GetActiveForPetAsync(Guid petId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(r => r.PetId == petId && r.IsActive, ct);
    public Task<bool> IsActiveAttendingVetAsync(Guid petId, Guid vetUserId, CancellationToken ct = default) =>
        Set.AnyAsync(r => r.PetId == petId && r.VetUserId == vetUserId && r.IsActive, ct);
    public async Task<IReadOnlyList<AttendingVetRelationship>> GetActiveByVetAndClinicAsync(Guid vet, Guid clinic, CancellationToken ct = default) =>
        await Set.Where(r => r.VetUserId == vet && r.ClinicId == clinic && r.IsActive).ToListAsync(ct);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public UnitOfWork(AppDbContext db) => _db = db;
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public void DiscardUnsavedChanges()
    {
        foreach (var e in _db.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList())
        {
            if (e.State == EntityState.Added) e.State = EntityState.Detached;
            else { e.CurrentValues.SetValues(e.OriginalValues); e.State = EntityState.Unchanged; }
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        if (_db.Database.CurrentTransaction != null) return await action();
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await action();
            await tx.CommitAsync(ct);
            return result;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
