using System.Net;

namespace VetPlatform.Web.Tests;

/// <summary>Pruebas del flujo Sprint 1 tal como lo vive un navegador: vistas + cookies + antiforgery.</summary>
public class Sprint1FlowTests : IClassFixture<VetPlatformWebFactory>
{
    private readonly VetPlatformWebFactory _factory;

    public Sprint1FlowTests(VetPlatformWebFactory factory) => _factory = factory;

    // ---------- helpers (compartidos en WebTestHelpers) ----------

    private HttpClient CreateClient() => WebTestHelpers.CreateClient(_factory);

    private static Task<(string Token, string Cookie)> GetFormAsync(HttpClient client, string url) =>
        WebTestHelpers.GetFormAsync(client, url);

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields) =>
        WebTestHelpers.Form(token, fields);

    // ---------- portada y páginas públicas ----------

    [Fact]
    public async Task Portada_renderiza()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Plataforma Centralizada de Servicios Veterinarios", html);
    }

    [Fact]
    public async Task Mascotas_requiere_sesion_y_redirige_a_login()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/Pets");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    // ---------- registro y login ----------

    [Fact]
    public async Task Registro_login_y_lista_de_mascotas_flujo_completo()
    {
        var client = CreateClient();
        var email = $"dueno{Guid.NewGuid():N}@test.com";

        // PBI-01: registrar propietario.
        var (registerToken, registerCookie) = await GetFormAsync(client, "/Account/Register");
        var registerResponse = await client.PostAsync("/Account/Register", Form(registerToken,
            ("FullName", "Juan Pérez"), ("Identification", "1-2345-6789"),
            ("Phone", "8888-8888"), ("Address", "San José"),
            ("Email", email),
            ("Password", "Secreta1"), ("ConfirmPassword", "Secreta1")));

        Assert.Equal(HttpStatusCode.Redirect, registerResponse.StatusCode);
        Assert.Equal("/Account/Login", registerResponse.Headers.Location!.ToString());

        // Login con las credenciales recién creadas.
        var (loginToken, loginCookie) = await GetFormAsync(client, "/Account/Login");
        var loginResponse = await client.PostAsync("/Account/Login", Form(loginToken,
            ("Email", email), ("Password", "Secreta1")));

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.Equal("/Pets", loginResponse.Headers.Location!.ToString());

        // La sesión quedó abierta (cookie de autenticación).
        var listResponse = await client.GetAsync("/Pets");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listHtml = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("Todavía no registraste ninguna mascota", listHtml);
    }

    [Fact]
    public async Task Login_con_credenciales_invalidas_muestra_error_en_la_vista()
    {
        var client = CreateClient();
        var (token, _) = await GetFormAsync(client, "/Account/Login");

        var response = await client.PostAsync("/Account/Login", Form(token,
            ("Email", "noexiste@test.com"), ("Password", "Incorrecta1")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("validation-summary", html);
    }

    // ---------- mascotas ----------

    [Fact]
    public async Task Registrar_mascota_luego_se_ve_en_la_lista_y_en_su_detalle()
    {
        var client = await RegisterAndLoginAsync(_factory);
        var petName = $"Toby{Guid.NewGuid():N}";

        // PBI-02/03: registrar mascota (el vínculo con el dueño se hace en el servidor).
        var (createToken, _) = await GetFormAsync(client, "/Pets/Create");
        var createResponse = await client.PostAsync("/Pets/Create", Form(createToken,
            ("Name", petName), ("Species", "Perro"), ("Breed", "Mestizo"), ("BirthDate", "2021-05-10")));

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        Assert.StartsWith("/Pets/Details/", createResponse.Headers.Location!.ToString());

        // Se ve en la lista (PBI-01).
        var listHtml = await (await client.GetAsync("/Pets")).Content.ReadAsStringAsync();
        Assert.Contains(petName, listHtml);

        // PBI-04: el detalle muestra la ficha y el historial (vacío al principio).
        var detailsHtml = await (await client.GetAsync(createResponse.Headers.Location!.ToString())).Content.ReadAsStringAsync();
        Assert.Contains(petName, detailsHtml);
        Assert.Contains("Perro", detailsHtml);
        Assert.Contains("Historial clínico", detailsHtml);
        Assert.Contains("Todavía no hay atenciones registradas", detailsHtml);

        // PBI-04 (Santiago): la edad se calcula desde la fecha de nacimiento.
        Assert.Contains("Edad", detailsHtml);
    }

    [Fact]
    public async Task Mascota_con_datos_invalidos_vuelve_al_formulario_con_errores()
    {
        var client = await RegisterAndLoginAsync(_factory);
        var (token, _) = await GetFormAsync(client, "/Pets/Create");

        var response = await client.PostAsync("/Pets/Create", Form(token,
            ("Name", ""), ("Species", ""), ("Breed", ""), ("BirthDate", "")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("El nombre es obligatorio", html);
        Assert.Contains("La especie es obligatoria", html);
    }

    // ---------- helper de sesión ----------

    /// <summary>Registra un propietario único y devuelve un cliente con la sesión iniciada.</summary>
    private static async Task<HttpClient> RegisterAndLoginAsync(VetPlatformWebFactory factory)
    {
        return await WebTestHelpers.RegisterAndLoginAsync(factory);
    }
}
