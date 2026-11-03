using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VetPlatform.Domain.Entities.Identity;

namespace VetPlatform.Infrastructure.Security;

public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "VetPlatform";
    public string Audience { get; set; } = "VetPlatform";
    public int ExpiryMinutes { get; set; } = 60;
}

public static class VetClaims
{
    public const string Organization = "org";
    public const string Verified = "verified";
}

public class JwtTokenGenerator
{
    private readonly JwtOptions _opt;
    private readonly TimeProvider _time;
    public JwtTokenGenerator(IOptions<JwtOptions> opt, TimeProvider time) { _opt = opt.Value; _time = time; }

    public (string Token, DateTimeOffset Expiry) Generate(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(VetClaims.Verified, user.IsVerified ? "true" : "false"),
        };
        foreach (var r in user.ActiveRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, r.Role.ToString()));
            if (r.OrganizationId.HasValue) claims.Add(new Claim(VetClaims.Organization, r.OrganizationId.Value.ToString()));
        }
        var expiry = _time.GetUtcNow().AddMinutes(_opt.ExpiryMinutes);
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_opt.Issuer, _opt.Audience, claims.DistinctBy(c => c.Type + c.Value),
            expires: expiry.UtcDateTime, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }
}
