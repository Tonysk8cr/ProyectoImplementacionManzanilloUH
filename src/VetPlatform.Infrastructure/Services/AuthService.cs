using FluentValidation;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Application.Features.Identity;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Entities.Identity;
using VetPlatform.Domain.Enums;
using VetPlatform.Domain.Interfaces.Repositories;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly JwtTokenGenerator _jwt;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _current;
    private readonly TimeProvider _time;
    private readonly IValidator<RegisterUserCommand> _registerValidator;
    private readonly IValidator<LoginCommand> _loginValidator;

    public AuthService(IUserRepository users, IUnitOfWork uow, IPasswordHasher hasher, JwtTokenGenerator jwt, IAuditService audit,
        ICurrentUserService current, TimeProvider time, IValidator<RegisterUserCommand> registerValidator, IValidator<LoginCommand> loginValidator)
    {
        _users = users; _uow = uow; _hasher = hasher; _jwt = jwt; _audit = audit; _current = current; _time = time;
        _registerValidator = registerValidator; _loginValidator = loginValidator;
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Email, u.FullName, u.IsVerified, u.IsSuspended, u.ActiveRoles.Select(r => r.Role.ToString()).Distinct().ToList());

    public async Task<Result<UserDto>> RegisterAsync(RegisterUserCommand cmd, CancellationToken ct = default)
    {
        var v = await _registerValidator.ValidateAsync(cmd, ct);
        if (!v.IsValid) return Result<UserDto>.Invalid(v.Errors.Select(e => e.ErrorMessage).ToArray());
        if (await _users.EmailExistsAsync(cmd.Email, ct)) return Result<UserDto>.Conflict("El email ya está registrado.");

        var now = _time.GetUtcNow();
        var user = new User
        {
            Email = cmd.Email.Trim().ToLowerInvariant(), FullName = cmd.FullName.Trim(), PasswordHash = _hasher.Hash(cmd.Password),
            // Professionals (vets, stores) require manual verification by an admin.
            IsVerified = cmd.Role == UserRole.PetOwner, CreatedAt = now, UpdatedAt = now,
        };
        user.UserRoles.Add(new UserRoleAssignment { UserId = user.Id, Role = cmd.Role, GrantedAt = now });
        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(user.Id, AuditAction.Create, "User", user.Id.ToString(), null, AuditLog.Results.Allowed, $"role={cmd.Role}", ct);
        return Result<UserDto>.Ok(ToDto(user));
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginCommand cmd, CancellationToken ct = default)
    {
        var v = await _loginValidator.ValidateAsync(cmd, ct);
        if (!v.IsValid) return Result<LoginResponse>.Invalid(v.Errors.Select(e => e.ErrorMessage).ToArray());
        var user = await _users.GetByEmailAsync(cmd.Email.Trim(), ct);
        if (user == null || !_hasher.Verify(cmd.Password, user.PasswordHash))
            return Result<LoginResponse>.Fail(ErrorType.Unauthorized, "Credenciales inválidas.");
        if (user.IsSuspended)
        {
            await _audit.LogAsync(user.Id, AuditAction.Login, "User", user.Id.ToString(), null, AuditLog.Results.Denied, "suspended", ct);
            return Result<LoginResponse>.Forbidden("La cuenta está suspendida.");
        }
        var (token, expiry) = _jwt.Generate(user);
        await _audit.LogAsync(user.Id, AuditAction.Login, "User", user.Id.ToString(), null, AuditLog.Results.Allowed, null, ct);
        return Result<LoginResponse>.Ok(new LoginResponse(token, expiry, user.Id, user.ActiveRoles.Select(r => r.Role.ToString()).Distinct().ToList()));
    }

    public async Task<Result<UserDto>> SuspendAsync(SuspendUserCommand cmd, CancellationToken ct = default)
    {
        if (_current.UserId is not { } actor || !_current.IsInRole(UserRole.PlatformAdmin)) return Result<UserDto>.Forbidden();
        var user = await _users.GetWithRolesAsync(cmd.UserId, ct);
        if (user == null) return Result<UserDto>.NotFound("Usuario no encontrado.");
        user.IsSuspended = true;
        user.UpdatedAt = _time.GetUtcNow();
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(actor, AuditAction.Suspend, "User", user.Id.ToString(), null, AuditLog.Results.Allowed, cmd.Reason, ct);
        return Result<UserDto>.Ok(ToDto(user));
    }

    public async Task<Result<UserDto>> VerifyAsync(Guid userId, CancellationToken ct = default)
    {
        if (_current.UserId is not { } actor || !_current.IsInRole(UserRole.PlatformAdmin)) return Result<UserDto>.Forbidden();
        var user = await _users.GetWithRolesAsync(userId, ct);
        if (user == null) return Result<UserDto>.NotFound("Usuario no encontrado.");
        user.IsVerified = true;
        user.UpdatedAt = _time.GetUtcNow();
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(actor, AuditAction.Update, "User", user.Id.ToString(), null, AuditLog.Results.Allowed, "verified", ct);
        return Result<UserDto>.Ok(ToDto(user));
    }
}
