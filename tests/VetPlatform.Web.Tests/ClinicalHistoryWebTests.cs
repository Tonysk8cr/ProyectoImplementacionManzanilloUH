using System.Net;
using Microsoft.Extensions.DependencyInjection;
using VetPlatform.Domain.Entities.Clinical;
using VetPlatform.Domain.Entities.Identity;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Data;
using VetPlatform.Infrastructure.Security;
using static VetPlatform.Web.Tests.WebTestHelpers;

namespace VetPlatform.Web.Tests;

/// <summary>Historial de consultas clínicas visto por el médico veterinario: vistas + cookies + datos reales en BD.</summary>
public class ClinicalHistoryWebTests : IClassFixture<VetPlatformWebFactory>
{
    private const string Password = "Secreta1";
    private readonly VetPlatformWebFactory _factory;
    public ClinicalHistoryWebTests(VetPlatformWebFactory factory) => _factory = factory;

    private sealed record Patient(Guid PetId, string Name, Guid[] ConsultationIds);

    private static string Fmt(DateTimeOffset d) => d.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    private async Task<(Guid Id, string Email)> SeedUserAsync(UserRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Email = $"{role}{Guid.NewGuid():N}@test.com".ToLowerInvariant(),
            FullName = $"Usuario {role}",
            PasswordHash = hasher.Hash(Password),
            IsVerified = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.UserRoles.Add(new UserRoleAssignment { UserId = user.Id, Role = role, GrantedAt = now });
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (user.Id, user.Email);
    }

    /// <summary>Mascota con expediente, veterinario regente (opcional) y consultas con las fechas y notas dadas.</summary>
    private async Task<Patient> SeedPatientAsync(Guid ownerId, Guid? vetId, string name, params (DateTimeOffset Date, string Notes)[] consultations)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var pet = new Pet { OwnerId = ownerId, Name = name, Species = "Perro", QrCode = Pet.GenerateOpaqueQrCode(), CreatedAt = now };
        var record = new ClinicalRecord { PetId = pet.Id, CreatedAt = now };
        db.Pets.Add(pet);
        db.ClinicalRecords.Add(record);
        if (vetId is { } vet)
            db.AttendingVetRelationships.Add(new AttendingVetRelationship { PetId = pet.Id, VetUserId = vet, EstablishedAt = now });
        var ids = new List<Guid>();
        foreach (var (date, notes) in consultations)
        {
            var c = new Consultation
            {
                ClinicalRecordId = record.Id,
                VetUserId = vetId ?? Guid.Empty,
                AttendanceDate = date,
                RegistrationDate = now,
                Notes = notes,
                Instructions = $"Indicaciones {notes}"
            };
            c.Prescriptions.Add(new Prescription
            {
                ConsultationId = c.Id,
                VetUserId = c.VetUserId,
                Medication = $"Fármaco {notes}",
                Dosage = "1 tableta",
                Route = "Oral",
                Frequency = "12h",
                DurationDays = 5,
                CreatedAt = now
            });
            db.Consultations.Add(c);
            ids.Add(c.Id);
        }
        await db.SaveChangesAsync();
        return new Patient(pet.Id, name, ids.ToArray());
    }

    private async Task<HttpClient> LoginAsync(string email)
    {
        var client = CreateClient(_factory);
        var (token, _) = await GetFormAsync(client, "/Account/Login");
        var response = await client.PostAsync("/Account/Login", Form(token, ("Email", email), ("Password", Password)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    private static async Task<string> HtmlAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    }

    private async Task<(HttpClient Client, Guid VetId, Guid OwnerId)> VetSessionAsync()
    {
        var vet = await SeedUserAsync(UserRole.AttendingVet);
        var owner = await SeedUserAsync(UserRole.PetOwner);
        return (await LoginAsync(vet.Email), vet.Id, owner.Id);
    }

    [Fact]
    public async Task Vet_patient_list_shows_only_own_patients()
    {
        var (client, vetId, ownerId) = await VetSessionAsync();
        var mine = await SeedPatientAsync(ownerId, vetId, $"Rocky{Guid.NewGuid():N}");
        var otherVet = await SeedUserAsync(UserRole.AttendingVet);
        var notMine = await SeedPatientAsync(ownerId, otherVet.Id, $"Luna{Guid.NewGuid():N}");

        var html = await HtmlAsync(client, "/Pets/Patients");

        Assert.Contains("Mis pacientes", html);
        Assert.Contains(mine.Name, html);
        Assert.Contains($"/Pets/Details/{mine.PetId}", html);
        Assert.DoesNotContain(notMine.Name, html);
    }

    [Fact]
    public async Task Vet_with_no_patients_sees_empty_state()
    {
        var (client, _, _) = await VetSessionAsync();

        var html = await HtmlAsync(client, "/Pets/Patients");

        Assert.Contains("No tienes pacientes asignados", html);
    }

    [Fact]
    public async Task Owner_cannot_open_vet_patient_list()
    {
        var owner = await SeedUserAsync(UserRole.PetOwner);
        var client = await LoginAsync(owner.Email);

        var response = await client.GetAsync("/Pets/Patients");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task History_shows_dates_most_recent_first_with_detail_links_and_patient_identity()
    {
        var (client, vetId, ownerId) = await VetSessionAsync();
        var oldest = new DateTimeOffset(2026, 1, 5, 9, 30, 0, TimeSpan.Zero);
        var newest = new DateTimeOffset(2026, 9, 20, 15, 0, 0, TimeSpan.Zero);
        var middle = new DateTimeOffset(2026, 5, 12, 11, 15, 0, TimeSpan.Zero);
        var p = await SeedPatientAsync(ownerId, vetId, $"Max{Guid.NewGuid():N}",
            (oldest, "Vacunación"), (newest, "Control dental"), (middle, "Dermatitis"));

        var html = await HtmlAsync(client, $"/Pets/Details/{p.PetId}");

        Assert.Contains($"Historial clínico de {p.Name}", html);
        var iNew = html.IndexOf(Fmt(newest), StringComparison.Ordinal);
        var iMid = html.IndexOf(Fmt(middle), StringComparison.Ordinal);
        var iOld = html.IndexOf(Fmt(oldest), StringComparison.Ordinal);
        Assert.True(iNew >= 0 && iMid >= 0 && iOld >= 0, "Todas las fechas deben mostrarse");
        Assert.True(iNew < iMid && iMid < iOld, "Orden cronológico: de la más reciente a la más antigua");
        foreach (var id in p.ConsultationIds)
            Assert.Contains($"/Pets/{p.PetId}/Consultations/{id}", html);
        Assert.Contains("/Pets/Patients", html); // volver a la lista de pacientes
    }

    [Fact]
    public async Task Patient_without_consultations_shows_empty_history()
    {
        var (client, vetId, ownerId) = await VetSessionAsync();
        var p = await SeedPatientAsync(ownerId, vetId, $"Nala{Guid.NewGuid():N}");

        var html = await HtmlAsync(client, $"/Pets/Details/{p.PetId}");

        Assert.Contains("Todavía no hay atenciones registradas", html);
    }

    [Fact]
    public async Task Consultation_detail_shows_full_information_and_patient()
    {
        var (client, vetId, ownerId) = await VetSessionAsync();
        var date = new DateTimeOffset(2026, 3, 8, 10, 45, 0, TimeSpan.Zero);
        var p = await SeedPatientAsync(ownerId, vetId, $"Kira{Guid.NewGuid():N}", (date, "Gastritis aguda"));

        var html = await HtmlAsync(client, $"/Pets/{p.PetId}/Consultations/{p.ConsultationIds[0]}");

        Assert.Contains(p.Name, html);
        Assert.Contains(Fmt(date), html);
        Assert.Contains("Gastritis aguda", html);
        Assert.Contains("Indicaciones Gastritis aguda", html);
        Assert.Contains("Fármaco Gastritis aguda", html);
        Assert.Contains($"/Pets/Details/{p.PetId}", html); // volver al historial del paciente
    }

    [Fact]
    public async Task Consultation_of_another_patient_is_not_shown_under_this_patient()
    {
        var (client, vetId, ownerId) = await VetSessionAsync();
        var a = await SeedPatientAsync(ownerId, vetId, $"Bruno{Guid.NewGuid():N}", (DateTimeOffset.UtcNow, "Notas de Bruno"));
        var b = await SeedPatientAsync(ownerId, vetId, $"Coco{Guid.NewGuid():N}", (DateTimeOffset.UtcNow, "Notas secretas de Coco"));

        var html = await HtmlAsync(client, $"/Pets/{a.PetId}/Consultations/{b.ConsultationIds[0]}");

        Assert.Contains("La consulta no pertenece a este paciente.", html);
        Assert.DoesNotContain("Notas secretas de Coco", html);
    }

    [Fact]
    public async Task Unknown_consultation_shows_understandable_error()
    {
        var (client, vetId, ownerId) = await VetSessionAsync();
        var p = await SeedPatientAsync(ownerId, vetId, $"Toby{Guid.NewGuid():N}");

        var html = await HtmlAsync(client, $"/Pets/{p.PetId}/Consultations/{Guid.NewGuid()}");

        Assert.Contains("Consulta no encontrada.", html);
        Assert.Contains("validation-summary-errors", html);
    }

    [Fact]
    public async Task Vet_without_relationship_cannot_see_history_or_detail()
    {
        var (client, _, ownerId) = await VetSessionAsync();
        var otherVet = await SeedUserAsync(UserRole.AttendingVet);
        var p = await SeedPatientAsync(ownerId, otherVet.Id, $"Ajeno{Guid.NewGuid():N}", (DateTimeOffset.UtcNow, "Notas privadas"));

        var history = await HtmlAsync(client, $"/Pets/Details/{p.PetId}");
        var detail = await HtmlAsync(client, $"/Pets/{p.PetId}/Consultations/{p.ConsultationIds[0]}");

        Assert.DoesNotContain("Notas privadas", history);
        Assert.DoesNotContain("Notas privadas", detail);
        Assert.Contains("No tiene autorización sobre este expediente.", detail);
    }

    [Fact]
    public async Task Switching_patient_does_not_keep_previous_history()
    {
        var (client, vetId, ownerId) = await VetSessionAsync();
        var a = await SeedPatientAsync(ownerId, vetId, $"Pelusa{Guid.NewGuid():N}", (DateTimeOffset.UtcNow, "Historial exclusivo de Pelusa"));
        var b = await SeedPatientAsync(ownerId, vetId, $"Bigotes{Guid.NewGuid():N}", (DateTimeOffset.UtcNow, "Historial exclusivo de Bigotes"));

        var htmlA = await HtmlAsync(client, $"/Pets/Details/{a.PetId}");
        var htmlB = await HtmlAsync(client, $"/Pets/Details/{b.PetId}");

        Assert.Contains("Historial exclusivo de Pelusa", htmlA);
        Assert.DoesNotContain("Historial exclusivo de Bigotes", htmlA);
        Assert.Contains("Historial exclusivo de Bigotes", htmlB);
        Assert.Contains($"Historial clínico de {b.Name}", htmlB);
        Assert.DoesNotContain("Historial exclusivo de Pelusa", htmlB);
        Assert.DoesNotContain(a.Name, htmlB);
    }

    [Fact]
    public async Task Owner_still_sees_history_and_back_link_to_own_pets()
    {
        var owner = await SeedUserAsync(UserRole.PetOwner);
        var client = await LoginAsync(owner.Email);
        var p = await SeedPatientAsync(owner.Id, null, $"Mia{Guid.NewGuid():N}", (DateTimeOffset.UtcNow, "Chequeo anual"));

        var history = await HtmlAsync(client, $"/Pets/Details/{p.PetId}");
        var detail = await HtmlAsync(client, $"/Pets/{p.PetId}/Consultations/{p.ConsultationIds[0]}");

        Assert.Contains("Chequeo anual", history);
        Assert.Contains("Volver a mis mascotas", history);
        Assert.Contains("Chequeo anual", detail);
    }
}
