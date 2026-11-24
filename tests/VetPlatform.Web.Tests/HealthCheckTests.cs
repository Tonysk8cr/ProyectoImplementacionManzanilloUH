using System.Net;
using static VetPlatform.Web.Tests.WebTestHelpers;

namespace VetPlatform.Web.Tests;

/// <summary>/health permite comprobar en Somee que la app llega a la base de datos.</summary>
public class HealthCheckTests : IClassFixture<VetPlatformWebFactory>
{
    private readonly VetPlatformWebFactory _factory;
    public HealthCheckTests(VetPlatformWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_reports_database_status_without_login()
    {
        using var client = CreateClient(_factory);

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ok\"", body);
        Assert.Contains("\"database\":\"ok\"", body);
    }
}
