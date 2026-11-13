using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VetPlatform.Infrastructure;
using VetPlatform.Infrastructure.Data;
using Xunit;

namespace VetPlatform.Application.Tests;

/// <summary>Integración con SQL Server (Somee): migraciones y configuración.</summary>
public class DatabaseIntegrationTests
{
    /// <summary>Falla si alguien cambia una entidad y olvida generar la migración.</summary>
    [Fact]
    public void Migrations_snapshot_matches_the_current_model()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused") // no se conecta: solo construye el modelo del proveedor
            .Options;
        using var db = new AppDbContext(options);

        var migrationsAssembly = db.GetService<IMigrationsAssembly>();
        var snapshot = migrationsAssembly.ModelSnapshot;
        snapshot.Should().NotBeNull("debe existir al menos una migración");

        var snapshotModel = db.GetService<IModelRuntimeInitializer>().Initialize(((IMutableModel)snapshot!.Model).FinalizeModel());
        var differences = db.GetService<IMigrationsModelDiffer>()
            .GetDifferences(snapshotModel.GetRelationalModel(), db.GetService<IDesignTimeModel>().Model.GetRelationalModel());

        differences.Should().BeEmpty("hay cambios de modelo sin migración: ejecute 'dotnet ef migrations add <Nombre>'");
    }

    [Fact]
    public void Migrations_target_sql_server()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=unused;Database=unused").Options);
        db.Database.GetMigrations().Should().NotBeEmpty();
        db.Database.IsSqlServer().Should().BeTrue();
    }

    private static IConfiguration Config(string? connection, string? jwtKey) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connection,
            ["Jwt:Key"] = jwtKey,
        }).Build();

    [Fact]
    public void Startup_fails_clearly_without_connection_string()
    {
        var act = () => new ServiceCollection().AddInfrastructure(Config(null, new string('k', 40)));

        act.Should().Throw<InvalidOperationException>().WithMessage("*DefaultConnection*");
    }

    [Fact]
    public void Startup_fails_clearly_without_jwt_key()
    {
        var act = () => new ServiceCollection().AddInfrastructure(Config("Server=x;Database=y", ""));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Key*");
    }
}
