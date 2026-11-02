using System.Net;
using System.Text.RegularExpressions;

namespace VetPlatform.Web.Tests;

/// <summary>Helpers compartidos por los tests web: cliente sin redirecciones,
/// formularios Razor con antiforgery y registro + login de un propietario único.</summary>
internal static class WebTestHelpers
{
    /// <summary>Cliente que no sigue redirecciones, para poder asertar sobre los 302 del flujo.</summary>
    public static HttpClient CreateClient(VetPlatformWebFactory factory) =>
        factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    /// <summary>Extrae el token antiforgery de un formulario Razor.</summary>
    public static string AntiforgeryToken(string html) =>
        Regex.Match(html, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""")
            .Groups[1].Value;

    /// <summary>Lee una cookie de ASP.NET desde la respuesta (para reenviarla como cabecera).</summary>
    public static string AntiforgeryCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.FirstOrDefault(c => c.StartsWith(".AspNetCore.Antiforgery")) ?? ""
            : "";

    public static async Task<(string Token, string Cookie)> GetFormAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        return (AntiforgeryToken(html), AntiforgeryCookie(response));
    }

    public static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields)
    {
        var data = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
        foreach (var (key, value) in fields)
        {
            data[key] = value;
        }

        return new FormUrlEncodedContent(data);
    }

    /// <summary>Registra un propietario único y devuelve un cliente con la sesión iniciada.</summary>
    public static async Task<HttpClient> RegisterAndLoginAsync(VetPlatformWebFactory factory)
    {
        var client = CreateClient(factory);
        var email = $"dueno{Guid.NewGuid():N}@test.com";

        var (registerToken, _) = await GetFormAsync(client, "/Account/Register");
        await client.PostAsync("/Account/Register", Form(registerToken,
            ("FullName", "María López"), ("Identification", "9-8765-4321"),
            ("Phone", "8777-7777"), ("Address", ""),
            ("Email", email),
            ("Password", "Secreta1"), ("ConfirmPassword", "Secreta1")));

        var (loginToken, _) = await GetFormAsync(client, "/Account/Login");
        var loginResponse = await client.PostAsync("/Account/Login", Form(loginToken,
            ("Email", email), ("Password", "Secreta1")));

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        return client;
    }
}
