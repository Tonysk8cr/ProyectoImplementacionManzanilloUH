using Microsoft.EntityFrameworkCore;
using VetPlatform.Domain.Entities.Identity;
using VetPlatform.Domain.Interfaces.Repositories;
using VetPlatform.Infrastructure.Data;

namespace VetPlatform.Infrastructure.Repositories;

public class OwnerProfileRepository : BaseRepository<OwnerProfile>, IOwnerProfileRepository
{
    public OwnerProfileRepository(AppDbContext db) : base(db) { }

    public Task<OwnerProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public Task<bool> IdentificationExistsAsync(string identificationNumber, Guid? excludingProfileId = null, CancellationToken ct = default) =>
        Set.AnyAsync(p => p.IdentificationNumber == identificationNumber
                          && (excludingProfileId == null || p.Id != excludingProfileId), ct);
}
