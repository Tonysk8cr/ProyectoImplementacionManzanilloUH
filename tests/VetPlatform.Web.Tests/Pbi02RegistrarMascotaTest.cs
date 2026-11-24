using System.Net;
using static VetPlatform.Web.Tests.WebTestHelpers;

namespace VetPlatform.Web.Tests;

/// <summary>
/// PBI-02: Como propietario, quiero registrar una mascota para mantener su información básica.
/// Cada test corresponde a un criterio de aceptación del PBI.
/// </summary>
public class Pbi02RegistrarMascotaTests : IClassFixture<VetPlatformWebFactory>
{
    private readonly VetPlatformWebFactory _factory;
    public Pbi02RegistrarMascotaTests(VetPlatformWebFactory factory) => _factory = factory;

    private static string NombreUnico() => $"Firulais-{Guid.NewGuid():N}"[..17];

    [Fact]
    public async Task Registro_valido_crea_la_mascota_y_muestra_su_detalle()
    {
        using var client = await RegisterAndLoginAsync(_factory);
        var nombre = NombreUnico();

        var (token, _) = await GetFormAsync(client, "/Pets/Create");
        var response = await client.PostAsync("/Pets/Create", Form(token,
            ("Name", nombre), ("Species", "Perro"),
            ("Breed", "Labrador"), ("BirthDate", "2022-03-15")));

        // Post-Redirect-Get: al guardar redirige al detalle de la mascota creada.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.Contains("/Pets/Details/", location);

        var detalle = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
        var html = WebUtility.HtmlDecode(await detalle.Content.ReadAsStringAsync());
        Assert.Contains(nombre, html);
        Assert.Contains("Perro", html);
        Assert.Contains("Labrador", html);
        Assert.Contains("15/03/2022", html);
        Assert.Contains("Código QR", html);
    }

    [Fact]
    public async Task Registro_solo_con_campos_obligatorios_es_valido()
    {
        using var client = await RegisterAndLoginAsync(_factory);

        var (token, _) = await GetFormAsync(client, "/Pets/Create");
        var response = await client.PostAsync("/Pets/Create", Form(token,
            ("Name", NombreUnico()), ("Species", "Gato"),
            ("Breed", ""), ("BirthDate", "")));

        // Raza y fecha de nacimiento son opcionales.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Pets/Details/", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Registro_sin_nombre_muestra_error_y_no_guarda()
    {
        using var client = await RegisterAndLoginAsync(_factory);

        var (token, _) = await GetFormAsync(client, "/Pets/Create");
        var response = await client.PostAsync("/Pets/Create", Form(token,
            ("Name", ""), ("Species", "Perro"),
            ("Breed", "Beagle"), ("BirthDate", "")));

        // Si no guarda, vuelve a mostrar el formulario (200) en lugar de redirigir.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains(">El nombre es obligatorio.</span>", html);
        // Conserva lo que el usuario ya había escrito.
        Assert.Contains("Beagle", html);
    }

    [Fact]
    public async Task Registro_sin_especie_muestra_error_y_no_guarda()
    {
        using var client = await RegisterAndLoginAsync(_factory);

        var (token, _) = await GetFormAsync(client, "/Pets/Create");
        var response = await client.PostAsync("/Pets/Create", Form(token,
            ("Name", NombreUnico()), ("Species", ""),
            ("Breed", ""), ("BirthDate", "")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains(">La especie es obligatoria.</span>", html);
    }

    [Fact]
    public async Task Registro_con_fecha_futura_muestra_error_y_no_guarda()
    {
        // Este test falla hasta implementar el Paso 1 (validación de fecha en PetViewModel).
        using var client = await RegisterAndLoginAsync(_factory);
        var manana = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");

        var (token, _) = await GetFormAsync(client, "/Pets/Create");
        var response = await client.PostAsync("/Pets/Create", Form(token,
            ("Name", NombreUnico()), ("Species", "Perro"),
            ("Breed", ""), ("BirthDate", manana)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains(">La fecha de nacimiento no puede ser futura.</span>", html);
    }

    [Fact]
    public async Task Mascota_registrada_aparece_en_mis_mascotas()
    {
        using var client = await RegisterAndLoginAsync(_factory);
        var nombre = NombreUnico();

        var (token, _) = await GetFormAsync(client, "/Pets/Create");
        await client.PostAsync("/Pets/Create", Form(token,
            ("Name", nombre), ("Species", "Conejo"),
            ("Breed", ""), ("BirthDate", "")));

        var lista = await client.GetAsync("/Pets");
        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        var html = WebUtility.HtmlDecode(await lista.Content.ReadAsStringAsync());
        Assert.Contains(nombre, html);
    }

    [Fact]
    public async Task Usuario_sin_sesion_no_puede_abrir_el_formulario()
    {
        using var client = CreateClient(_factory);

        var response = await client.GetAsync("/Pets/Create");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }
}