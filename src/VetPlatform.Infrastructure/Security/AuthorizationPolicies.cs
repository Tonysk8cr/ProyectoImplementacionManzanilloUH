using Microsoft.AspNetCore.Authorization;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Infrastructure.Security;

/// <summary>Coarse role policies. Resource-level rules (ownership, active regente, valid emergency access) are enforced in services.</summary>
public static class AuthorizationPolicies
{
    public const string RequireAttendingVetAccess = nameof(RequireAttendingVetAccess);
    public const string RequireEmergencyAccess = nameof(RequireEmergencyAccess);
    public const string RequireClinicAdmin = nameof(RequireClinicAdmin);
    public const string RequireStoreOperator = nameof(RequireStoreOperator);
    public const string RequirePetOwner = nameof(RequirePetOwner);
    public const string RequirePlatformAdmin = nameof(RequirePlatformAdmin);
    public const string RequireVet = nameof(RequireVet);

    public static void Configure(AuthorizationOptions o)
    {
        o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(); // deny by default
        o.AddPolicy(RequireAttendingVetAccess, p => p.RequireRole(nameof(UserRole.AttendingVet)));
        o.AddPolicy(RequireEmergencyAccess, p => p.RequireRole(nameof(UserRole.EmergencyVet)).RequireClaim(VetClaims.Verified, "true"));
        o.AddPolicy(RequireClinicAdmin, p => p.RequireRole(nameof(UserRole.ClinicAdmin), nameof(UserRole.PlatformAdmin)));
        o.AddPolicy(RequireStoreOperator, p => p.RequireRole(nameof(UserRole.StoreOperator)));
        o.AddPolicy(RequirePetOwner, p => p.RequireRole(nameof(UserRole.PetOwner)));
        o.AddPolicy(RequirePlatformAdmin, p => p.RequireRole(nameof(UserRole.PlatformAdmin)));
        o.AddPolicy(RequireVet, p => p.RequireRole(nameof(UserRole.AttendingVet), nameof(UserRole.EmergencyVet), nameof(UserRole.ExternalVet)));
    }
}
