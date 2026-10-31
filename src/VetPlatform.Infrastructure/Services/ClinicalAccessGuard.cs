using VetPlatform.Application.Common;
using VetPlatform.Domain.Entities.Clinical;
using VetPlatform.Domain.Enums;
using VetPlatform.Domain.Interfaces.Repositories;

namespace VetPlatform.Infrastructure.Services;

public enum ClinicalAccessLevel { None, Owner, AttendingVet, EmergencyVet }

/// <summary>Centralised, deny-by-default authorization for clinical data.</summary>
public class ClinicalAccessGuard
{
    private readonly IAttendingVetRelationshipRepository _rel;
    private readonly IEmergencyAccessRepository _emergency;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public ClinicalAccessGuard(IAttendingVetRelationshipRepository rel, IEmergencyAccessRepository emergency, IUnitOfWork uow, TimeProvider time)
    { _rel = rel; _emergency = emergency; _uow = uow; _time = time; }

    public async Task<ClinicalAccessLevel> GetReadAccessAsync(Pet pet, Guid userId, IReadOnlyCollection<UserRole> roles, CancellationToken ct = default)
    {
        if (pet.OwnerId == userId) return ClinicalAccessLevel.Owner;
        if (await CanWriteAsAttendingVetAsync(pet.Id, userId, roles, ct)) return ClinicalAccessLevel.AttendingVet;
        if (await HasValidEmergencyAccessAsync(pet.Id, userId, roles, ct)) return ClinicalAccessLevel.EmergencyVet;
        return ClinicalAccessLevel.None;
    }

    public async Task<bool> CanWriteAsAttendingVetAsync(Guid petId, Guid userId, IReadOnlyCollection<UserRole> roles, CancellationToken ct = default) =>
        roles.Contains(UserRole.AttendingVet) && await _rel.IsActiveAttendingVetAsync(petId, userId, ct);

    /// <summary>Checks the emergency access state on every call; persists expiration transitions.</summary>
    public async Task<bool> HasValidEmergencyAccessAsync(Guid petId, Guid userId, IReadOnlyCollection<UserRole> roles, CancellationToken ct = default)
    {
        if (!roles.Contains(UserRole.EmergencyVet)) return false;
        var access = await _emergency.GetLatestActiveAsync(petId, userId, ct);
        if (access == null) return false;
        if (access.IsValidAt(_time.GetUtcNow())) return true;
        await _uow.SaveChangesAsync(ct); // persist Expired status
        return false;
    }
}
