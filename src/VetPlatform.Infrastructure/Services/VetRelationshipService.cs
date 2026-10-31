using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Application.Features.Organizations;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Entities.Clinical;
using VetPlatform.Domain.Enums;
using VetPlatform.Domain.Interfaces.Repositories;

namespace VetPlatform.Infrastructure.Services;

public class VetRelationshipService : IVetRelationshipService
{
    private readonly IPetRepository _pets;
    private readonly IClinicRepository _clinics;
    private readonly IAttendingVetRelationshipRepository _relationships;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public VetRelationshipService(IPetRepository pets, IClinicRepository clinics, IAttendingVetRelationshipRepository relationships,
        INotificationService notifications, IAuditService audit, ICurrentUserService user, IUnitOfWork uow, TimeProvider time)
    { _pets = pets; _clinics = clinics; _relationships = relationships; _notifications = notifications; _audit = audit; _user = user; _uow = uow; _time = time; }

    public async Task<Result<AttendingVetRelationshipDto>> AssignAsync(AssignAttendingVetCommand cmd, CancellationToken ct = default)
    {
        var pet = await _pets.GetByIdAsync(cmd.PetId, ct);
        if (pet == null) return Result<AttendingVetRelationshipDto>.NotFound("Mascota no encontrada.");
        if (pet.OwnerId != _user.UserId) return Result<AttendingVetRelationshipDto>.Forbidden("Solo el tutor puede autorizar al regente.");
        var clinic = await _clinics.GetByIdAsync(cmd.ClinicId, ct);
        if (clinic == null || clinic.Status != ClinicStatus.Active) return Result<AttendingVetRelationshipDto>.Invalid("La clínica no está activa.");
        if (await _clinics.GetActiveMemberAsync(clinic.Id, cmd.VetUserId, ct) == null)
            return Result<AttendingVetRelationshipDto>.Invalid("El veterinario no es integrante activo de la clínica.");

        var rel = await _uow.ExecuteInTransactionAsync(async () =>
        {
            var now = _time.GetUtcNow();
            var current = await _relationships.GetActiveForPetAsync(pet.Id, ct);
            if (current != null)
            {
                current.Revoke(now); // single active regente per pet
                await _notifications.EnqueueAsync(current.VetUserId, "Relación de regencia finalizada", $"Ya no es regente de {pet.Name}.", pet.Id, "Pet", ct);
                await _uow.SaveChangesAsync(ct);
            }
            var r = new AttendingVetRelationship { PetId = pet.Id, VetUserId = cmd.VetUserId, ClinicId = clinic.Id, EstablishedAt = now, IsActive = true };
            await _relationships.AddAsync(r, ct);
            await _notifications.EnqueueAsync(cmd.VetUserId, "Nueva regencia", $"Fue asignado como regente de {pet.Name}.", pet.Id, "Pet", ct);
            await _uow.SaveChangesAsync(ct);
            return r;
        }, ct);
        await _audit.LogAsync(pet.OwnerId, AuditAction.AccessGranted, "AttendingVetRelationship", rel.Id.ToString(), clinic.Id, AuditLog.Results.Allowed, null, ct);
        return Result<AttendingVetRelationshipDto>.Ok(rel.ToDto());
    }

    public async Task<Result<bool>> RevokeAsync(RevokeAttendingVetCommand cmd, CancellationToken ct = default)
    {
        var pet = await _pets.GetByIdAsync(cmd.PetId, ct);
        if (pet == null) return Result<bool>.NotFound("Mascota no encontrada.");
        if (pet.OwnerId != _user.UserId) return Result<bool>.Forbidden("Solo el tutor puede revocar al regente.");
        var current = await _relationships.GetActiveForPetAsync(pet.Id, ct);
        if (current == null) return Result<bool>.NotFound("No hay regente activo.");
        current.Revoke(_time.GetUtcNow());
        await _notifications.EnqueueAsync(current.VetUserId, "Regencia revocada", $"El tutor revocó su regencia de {pet.Name}.", pet.Id, "Pet", ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(pet.OwnerId, AuditAction.Delete, "AttendingVetRelationship", current.Id.ToString(), current.ClinicId, AuditLog.Results.Allowed, null, ct);
        return Result<bool>.Ok(true);
    }
}
