using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Application.Features.Organizations;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Entities.Identity;
using VetPlatform.Domain.Entities.Organizations;
using VetPlatform.Domain.Enums;
using VetPlatform.Domain.Interfaces.Repositories;

namespace VetPlatform.Infrastructure.Services;

public class ClinicService : IClinicService
{
    private readonly IClinicRepository _clinics;
    private readonly IUserRepository _users;
    private readonly IAttendingVetRelationshipRepository _relationships;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public ClinicService(IClinicRepository clinics, IUserRepository users, IAttendingVetRelationshipRepository relationships,
        IAuditService audit, ICurrentUserService user, IUnitOfWork uow, TimeProvider time)
    { _clinics = clinics; _users = users; _relationships = relationships; _audit = audit; _user = user; _uow = uow; _time = time; }

    private bool IsClinicAdmin(Clinic c) => _user.IsInRole(UserRole.PlatformAdmin) || c.RepresentativeUserId == _user.UserId ||
        (_user.IsInRole(UserRole.ClinicAdmin) && _user.OrganizationIds.Contains(c.Id));

    public async Task<Result<ClinicDto>> CreateClinicAsync(CreateClinicCommand cmd, CancellationToken ct = default)
    {
        if (_user.UserId is not { } actor || !_user.IsInRole(UserRole.PlatformAdmin)) return Result<ClinicDto>.Forbidden();
        if (string.IsNullOrWhiteSpace(cmd.Name)) return Result<ClinicDto>.Invalid("El nombre es obligatorio.");
        var rep = await _users.GetByIdAsync(cmd.RepresentativeUserId, ct);
        if (rep == null) return Result<ClinicDto>.NotFound("Representante no encontrado.");
        var now = _time.GetUtcNow();
        var clinic = new Clinic { Name = cmd.Name.Trim(), RepresentativeUserId = rep.Id, Address = cmd.Address, Phone = cmd.Phone, Status = ClinicStatus.Active, CreatedAt = now };
        await _clinics.AddAsync(clinic, ct);
        await _users.AddRoleAsync(new UserRoleAssignment { UserId = rep.Id, Role = UserRole.ClinicAdmin, OrganizationId = clinic.Id, GrantedAt = now }, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(actor, AuditAction.Create, "Clinic", clinic.Id.ToString(), clinic.Id, AuditLog.Results.Allowed, null, ct);
        return Result<ClinicDto>.Ok(clinic.ToDto());
    }

    public async Task<Result<ClinicDto>> GetClinicAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _clinics.GetWithMembersAsync(id, ct);
        return c == null ? Result<ClinicDto>.NotFound("Clínica no encontrada.") : Result<ClinicDto>.Ok(c.ToDto());
    }

    public async Task<Result<ClinicMemberDto>> AddMemberAsync(AddMemberCommand cmd, CancellationToken ct = default)
    {
        var clinic = await _clinics.GetByIdAsync(cmd.ClinicId, ct);
        if (clinic == null) return Result<ClinicMemberDto>.NotFound("Clínica no encontrada.");
        if (!IsClinicAdmin(clinic)) return Result<ClinicMemberDto>.Forbidden();
        var vet = await _users.GetWithRolesAsync(cmd.UserId, ct);
        if (vet == null) return Result<ClinicMemberDto>.NotFound("Usuario no encontrado.");
        if (await _clinics.GetActiveMemberAsync(clinic.Id, vet.Id, ct) != null) return Result<ClinicMemberDto>.Conflict("El usuario ya es integrante.");
        var now = _time.GetUtcNow();
        var m = new ClinicMember { ClinicId = clinic.Id, UserId = vet.Id, JoinedAt = now };
        await _clinics.AddMemberAsync(m, ct);
        if (!vet.HasRole(UserRole.AttendingVet))
            await _users.AddRoleAsync(new UserRoleAssignment { UserId = vet.Id, Role = UserRole.AttendingVet, OrganizationId = clinic.Id, GrantedAt = now }, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_user.UserId!.Value, AuditAction.Create, "ClinicMember", m.Id.ToString(), clinic.Id, AuditLog.Results.Allowed, null, ct);
        return Result<ClinicMemberDto>.Ok(m.ToDto());
    }

    public async Task<Result<bool>> RemoveMemberAsync(RemoveMemberCommand cmd, CancellationToken ct = default)
    {
        var clinic = await _clinics.GetByIdAsync(cmd.ClinicId, ct);
        if (clinic == null) return Result<bool>.NotFound("Clínica no encontrada.");
        if (!IsClinicAdmin(clinic)) return Result<bool>.Forbidden();
        var m = await _clinics.GetActiveMemberAsync(clinic.Id, cmd.UserId, ct);
        if (m == null) return Result<bool>.NotFound("Integrante no encontrado.");
        var now = _time.GetUtcNow();
        m.LeftAt = now;
        // Leaving the clinic ends the regente relationships established through it.
        foreach (var r in await _relationships.GetActiveByVetAndClinicAsync(cmd.UserId, clinic.Id, ct)) r.Revoke(now);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_user.UserId!.Value, AuditAction.Delete, "ClinicMember", m.Id.ToString(), clinic.Id, AuditLog.Results.Allowed, null, ct);
        return Result<bool>.Ok(true);
    }
}
