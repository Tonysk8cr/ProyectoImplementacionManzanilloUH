using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VetPlatform.Infrastructure.Data;

namespace VetPlatform.Web.Tests;

/// <summary>
/// Arranca la aplicación MVC real (pipeline completo: antiforgery, cookies, vistas Razor)
/// reemplazando PostgreSQL por SQLite in-memory, para poder probar el flujo sin base de datos externa.
/// </summary>
public sealed class VetPlatformWebFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseSetting("ConnectionStrings:DefaultConnection", "unused");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");

        builder.ConfigureServices(services =>
        {
            // Sustituye el DbContext de PostgreSQL por SQLite in-memory.
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));

            // Asegura el esquema antes de la primera petición.
            var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}
