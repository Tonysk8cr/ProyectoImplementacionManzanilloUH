using System.Net;
using static VetPlatform.Web.Tests.WebTestHelpers;

namespace VetPlatform.Web.Tests;

public class Pbi05PruebasTests : IClassFixture<VetPlatformWebFactory>
{
    private readonly VetPlatformWebFactory _factory;
    public Pbi05PruebasTests(VetPlatformWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Registro_sin_identificacion_ni_telefono_muestra_errores_y_no_crea_cuenta()
    {
        using var client = CreateClient(_factory);
        var email = $"pbi05{Guid.NewGuid():N}@test.com";
        var (token, _) = await GetFormAsync(client, "/Account/Register");
        var response = await client.PostAsync("/Account/Register", Form(token,
            ("FullName", "Prueba QA"), ("Email", email),
            ("Password", "Secreta1"), ("ConfirmPassword", "Secreta1")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains(">La identificación es obligatoria.</span>", html);
        Assert.Contains(">El teléfono es obligatorio.</span>", html);
        Assert.Contains(email, html);

        var (loginToken, _) = await GetFormAsync(client, "/Account/Login");
        var login = await client.PostAsync("/Account/Login", Form(loginToken,
            ("Email", email), ("Password", "Secreta1")));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var pets = await client.GetAsync("/Pets");
        Assert.Equal(HttpStatusCode.Redirect, pets.StatusCode);
        Assert.Contains("/Account/Login", pets.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Consulta_de_mascota_inexistente_muestra_error_comprensible()
    {
        using var client = await RegisterAndLoginAsync(_factory);
        var response = await client.GetAsync($"/Pets/Details/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Mascota no encontrada.", html);
        Assert.Contains("validation-summary-errors", html);
    }
}
