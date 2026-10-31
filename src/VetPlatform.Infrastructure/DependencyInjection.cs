using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Audit;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Application.Features.Emergency;
using VetPlatform.Application.Features.Identity;
using VetPlatform.Application.Features.Marketplace;
using VetPlatform.Application.Features.Organizations;
using VetPlatform.Application.Features.Scheduling;
using VetPlatform.Domain.Interfaces.Repositories;
using VetPlatform.Infrastructure.Data;
using VetPlatform.Infrastructure.Repositories;
using VetPlatform.Infrastructure.Security;
using VetPlatform.Infrastructure.Services;

namespace VetPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(config.GetConnectionString("DefaultConnection")));
        services.AddInfrastructureCore(config);

        var jwt = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwt.Issuer, ValidateAudience = true, ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1),
            };
        });
        services.AddAuthorization(AuthorizationPolicies.Configure);
        services.AddHostedService<NotificationDispatcherHostedService>();
        return services;
    }

    /// <summary>Registers repositories and services (without DbContext provider / auth) — reused by tests.</summary>
    public static IServiceCollection AddInfrastructureCore(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtOptions>(config.GetSection("Jwt"));
        services.Configure<EmergencyAccessOptions>(config.GetSection("EmergencyAccess"));
        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IClinicalRecordRepository, ClinicalRecordRepository>();
        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IEmergencyAccessRepository, EmergencyAccessRepository>();
        services.AddScoped<IEmergencyAttendanceRepository, EmergencyAttendanceRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IAvailabilitySlotRepository, AvailabilitySlotRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAttendingVetRelationshipRepository, AttendingVetRelationshipRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<JwtTokenGenerator>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ClinicalAccessGuard>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationSender, LoggingNotificationSender>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClinicalService, ClinicalService>();
        services.AddScoped<IClinicService, ClinicService>();
        services.AddScoped<IVetRelationshipService, VetRelationshipService>();
        services.AddScoped<IEmergencyService, EmergencyService>();
        services.AddScoped<IMarketplaceService, MarketplaceService>();
        services.AddScoped<ISchedulingService, SchedulingService>();
        return services;
    }
}
