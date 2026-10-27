using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VetPlatform.Application.Common;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    public CurrentUserService(IHttpContextAccessor http) => _http = http;
    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public IReadOnlyCollection<UserRole> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => Enum.TryParse<UserRole>(c.Value, out var r) ? (UserRole?)r : null)
            .Where(r => r.HasValue).Select(r => r!.Value).ToList() ?? new List<UserRole>();
    public IReadOnlyCollection<Guid> OrganizationIds =>
        User?.FindAll(VetClaims.Organization).Select(c => Guid.Parse(c.Value)).ToList() ?? new List<Guid>();
    public Guid? OrganizationId => OrganizationIds.Count > 0 ? OrganizationIds.First() : null;
}
