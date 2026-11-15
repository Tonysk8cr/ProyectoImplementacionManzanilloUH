using FluentValidation;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Application.Features.Identity;
using VetPlatform.Application.Features.Owners;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Entities.Identity;
using VetPlatform.Domain.Enums;
using VetPlatform.Domain.Interfaces.Repositories;

namespace VetPlatform.Infrastructure.Services;

public class OwnerService : IOwnerService
{
    private readonly IOwnerProfileRepository _profiles;
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _user;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly IAuthService _auth;
    private readonly TimeProvider _time;
    private readonly IValidator<CreateOwnerProfileCommand> _createValidator;
    private readonly IValidator<UpdateOwnerProfileCommand> _updateValidator;
    private readonly IValidator<RegisterOwnerCommand> _registerValidator;

    public OwnerService(IOwnerProfileRepository profiles, IUserRepository users, ICurrentUserService user, IUnitOfWork uow,
        IAuditService audit, IAuthService auth, TimeProvider time, IValidator<CreateOwnerProfileCommand> createValidator,
        IValidator<UpdateOwnerProfileCommand> updateValidator, IValidator<RegisterOwnerCommand> registerValidator)
    {
        _profiles = profiles; _users = users; _user = user; _uow = uow; _audit = audit; _auth = auth; _time = time;
        _createValidator = createValidator; _updateValidator = updateValidator; _registerValidator = registerValidator;
    }

    private static string NormalizeId(string value) => value.Trim().ToUpperInvariant();

    private static OwnerProfileDto ToDto(OwnerProfile p, User u) =>
        new(p.Id, p.UserId, u.FullName, u.Email, p.IdentificationNumber, p.Phone, p.Address, p.CreatedAt, p.UpdatedAt);

    public async Task<Result<UserDto>> RegisterOwnerAsync(RegisterOwnerCommand cmd, CancellationToken ct = default)
    {
        var v = await _registerValidator.ValidateAsync(cmd, ct);
        if (!v.IsValid) return Result<UserDto>.Invalid(v.Errors.Select(e => e.ErrorMessage).ToArray());

        var identification = NormalizeId(cmd.IdentificationNumber);
        if (await _profiles.IdentificationExistsAsync(identification, null, ct))
            return Result<UserDto>.Conflict("Ya existe un propietario registrado con esa identificación.");

        // 1) Cuenta: reutiliza el registro existente (valida correo, contraseña y nombre) y siempre crea un PetOwner.
        var registered = await _auth.RegisterAsync(
            new RegisterUserCommand(cmd.Email, cmd.Password, cmd.FullName, UserRole.PetOwner), ct);
        if (!registered.Success || registered.Value is null) return registered;

        // 2) Datos del propietario. Si este paso fallara, la cuenta ya existe y el propietario
        //    puede completar sus datos después desde "Mi perfil".
        var now = _time.GetUtcNow();
        var profile = new OwnerProfile
        {
            UserId = registered.Value.Id,
            IdentificationNumber = identification,
            Phone = cmd.Phone.Trim(),
            Address = string.IsNullOrWhiteSpace(cmd.Address) ? null : cmd.Address.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        await _profiles.AddAsync(profile, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(registered.Value.Id, AuditAction.Create, "OwnerProfile", profile.Id.ToString(), null, AuditLog.Results.Allowed, null, ct);
        return registered;
    }

    public async Task<Result<OwnerProfileDto>> GetMyProfileAsync(CancellationToken ct = default)
    {
        if (_user.UserId is not { } actor)
            return Result<OwnerProfileDto>.Fail(ErrorType.Unauthorized, "Debe iniciar sesión.");

        var profile = await _profiles.GetByUserIdAsync(actor, ct);
        if (profile is null) return Result<OwnerProfileDto>.NotFound("Todavía no has registrado tus datos de propietario.");

        var user = await _users.GetByIdAsync(actor, ct);
        if (user is null) return Result<OwnerProfileDto>.NotFound("Usuario no encontrado.");

        return Result<OwnerProfileDto>.Ok(ToDto(profile, user));
    }

    public async Task<Result<OwnerProfileDto>> CreateMyProfileAsync(CreateOwnerProfileCommand cmd, CancellationToken ct = default)
    {
        if (_user.UserId is not { } actor || !_user.IsInRole(UserRole.PetOwner))
            return Result<OwnerProfileDto>.Forbidden("Solo un propietario puede registrar sus datos.");

        var v = await _createValidator.ValidateAsync(cmd, ct);
        if (!v.IsValid) return Result<OwnerProfileDto>.Invalid(v.Errors.Select(e => e.ErrorMessage).ToArray());

        if (await _profiles.GetByUserIdAsync(actor, ct) is not null)
            return Result<OwnerProfileDto>.Conflict("Ya registraste tus datos de propietario. Puedes editarlos.");

        var identification = NormalizeId(cmd.IdentificationNumber);
        if (await _profiles.IdentificationExistsAsync(identification, null, ct))
            return Result<OwnerProfileDto>.Conflict("Ya existe un propietario registrado con esa identificación.");

        var user = await _users.GetByIdAsync(actor, ct);
        if (user is null) return Result<OwnerProfileDto>.NotFound("Usuario no encontrado.");

        var now = _time.GetUtcNow();
        var profile = new OwnerProfile
        {
            UserId = actor,
            IdentificationNumber = identification,
            Phone = cmd.Phone.Trim(),
            Address = string.IsNullOrWhiteSpace(cmd.Address) ? null : cmd.Address.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        await _profiles.AddAsync(profile, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(actor, AuditAction.Create, "OwnerProfile", profile.Id.ToString(), null, AuditLog.Results.Allowed, null, ct);
        return Result<OwnerProfileDto>.Ok(ToDto(profile, user));
    }

    public async Task<Result<OwnerProfileDto>> UpdateMyProfileAsync(UpdateOwnerProfileCommand cmd, CancellationToken ct = default)
    {
        if (_user.UserId is not { } actor || !_user.IsInRole(UserRole.PetOwner))
            return Result<OwnerProfileDto>.Forbidden("Solo un propietario puede editar sus datos.");

        var v = await _updateValidator.ValidateAsync(cmd, ct);
        if (!v.IsValid) return Result<OwnerProfileDto>.Invalid(v.Errors.Select(e => e.ErrorMessage).ToArray());

        var profile = await _profiles.GetByUserIdAsync(actor, ct);
        if (profile is null) return Result<OwnerProfileDto>.NotFound("Todavía no has registrado tus datos de propietario.");

        var identification = NormalizeId(cmd.IdentificationNumber);
        if (identification != profile.IdentificationNumber
            && await _profiles.IdentificationExistsAsync(identification, profile.Id, ct))
            return Result<OwnerProfileDto>.Conflict("Ya existe un propietario registrado con esa identificación.");

        var user = await _users.GetByIdAsync(actor, ct);
        if (user is null) return Result<OwnerProfileDto>.NotFound("Usuario no encontrado.");

        profile.IdentificationNumber = identification;
        profile.Phone = cmd.Phone.Trim();
        profile.Address = string.IsNullOrWhiteSpace(cmd.Address) ? null : cmd.Address.Trim();
        profile.UpdatedAt = _time.GetUtcNow();
        await _profiles.UpdateAsync(profile, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(actor, AuditAction.Update, "OwnerProfile", profile.Id.ToString(), null, AuditLog.Results.Allowed, null, ct);
        return Result<OwnerProfileDto>.Ok(ToDto(profile, user));
    }

    public async Task<Result<bool>> DeleteMyProfileAsync(CancellationToken ct = default)
    {
        if (_user.UserId is not { } actor || !_user.IsInRole(UserRole.PetOwner))
            return Result<bool>.Forbidden("Solo un propietario puede eliminar sus datos.");

        var profile = await _profiles.GetByUserIdAsync(actor, ct);
        if (profile is null) return Result<bool>.NotFound("No hay datos de propietario para eliminar.");

        // Eliminación lógica (IsDeleted), igual que el resto de entidades del proyecto.
        await _profiles.DeleteAsync(profile, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(actor, AuditAction.Delete, "OwnerProfile", profile.Id.ToString(), null, AuditLog.Results.Allowed, null, ct);
        return Result<bool>.Ok(true);
    }
}
