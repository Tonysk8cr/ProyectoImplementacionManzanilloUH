using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VetPlatform.Application;
using VetPlatform.Application.Common;
using VetPlatform.Domain.Entities.Identity;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure;
using VetPlatform.Infrastructure.Data;

namespace VetPlatform.Application.Tests;

public class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
    public void Advance(TimeSpan t) => Now += t;
}

public class TestCurrentUser : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public List<UserRole> RoleList { get; set; } = new();
    public IReadOnlyCollection<UserRole> Roles => RoleList;
    public List<Guid> Orgs { get; set; } = new();
    public Guid? OrganizationId => Orgs.FirstOrDefault() is var g && g != Guid.Empty ? g : null;
    public IReadOnlyCollection<Guid> OrganizationIds => Orgs;
    public void As(Guid userId, params UserRole[] roles) { UserId = userId; RoleList = roles.ToList(); Orgs = new(); }
}

/// <summary>Real services + EF Core over SQLite in-memory (supports transactions and concurrency tokens).</summary>
public sealed class TestFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _root;
    public TestClock Clock { get; } = new();
    public TestCurrentUser CurrentUser { get; } = new();

    public TestFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "TEST_SECRET_KEY_THAT_IS_LONG_ENOUGH_123456",
            ["EmergencyAccess:DefaultExpiryMinutes"] = "120",
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructureCore(config);
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<ICurrentUserService>(CurrentUser);
        _root = services.BuildServiceProvider();
        using var scope = _root.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    /// <summary>Each scope = one request with its own DbContext.</summary>
    public IServiceScope NewScope() => _root.CreateScope();
    public T Get<T>(IServiceScope s) where T : notnull => s.ServiceProvider.GetRequiredService<T>();

    public async Task<Guid> SeedUserAsync(UserRole role, bool verified = true, Guid? orgId = null)
    {
        using var s = NewScope();
        var db = Get<AppDbContext>(s);
        var u = new User { Email = $"{Guid.NewGuid():N}@test.com", FullName = role.ToString(), PasswordHash = "x", IsVerified = verified, CreatedAt = Clock.Now, UpdatedAt = Clock.Now };
        u.UserRoles.Add(new UserRoleAssignment { UserId = u.Id, Role = role, OrganizationId = orgId, GrantedAt = Clock.Now });
        db.Users.Add(u);
        await db.SaveChangesAsync();
        return u.Id;
    }

    public async Task<T> RunAs<TSvc, T>(Guid userId, UserRole[] roles, Func<TSvc, Task<T>> action) where TSvc : notnull
    {
        CurrentUser.As(userId, roles);
        using var s = NewScope();
        return await action(Get<TSvc>(s));
    }

    public void Dispose() { _root.Dispose(); _connection.Dispose(); }
}
