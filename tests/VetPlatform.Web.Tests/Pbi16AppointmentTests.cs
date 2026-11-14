using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VetPlatform.Domain.Entities.Identity;
using VetPlatform.Domain.Entities.Organizations;
using VetPlatform.Domain.Entities.Scheduling;
using VetPlatform.Infrastructure.Data;

namespace VetPlatform.Web.Tests;

public class Pbi16AppointmentTests : IClassFixture<VetPlatformWebFactory>
{
    private readonly VetPlatformWebFactory _factory;
    public Pbi16AppointmentTests(VetPlatformWebFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, Guid Pet, Guid Slot, DateOnly Date)> Prepare()
    {
        var client = await WebTestHelpers.RegisterAndLoginAsync(_factory);
        var (token, _) = await WebTestHelpers.GetFormAsync(client, "/Pets/Create");
        var response = await client.PostAsync("/Pets/Create", WebTestHelpers.Form(token, ("Name", "Luna"), ("Species", "Gato")));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var petId = Guid.Parse(response.Headers.Location!.ToString().Split('/').Last());
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var vet = new User { FullName = "Veterinaria Ana", Email = $"{Guid.NewGuid()}@test.com", PasswordHash = "x" };
        var clinic = new Clinic { Name = "Clínica de Luna", RepresentativeUserId = vet.Id };
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        // 23:30 local occurs on the next UTC day; the screen must retain the chosen local date.
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(23, 30)), TimeSpan.FromHours(-6)).ToUniversalTime();
        var slot = new AvailabilitySlot { ClinicId = clinic.Id, VetUserId = vet.Id, StartTime = start, EndTime = start.AddMinutes(30) };
        db.Users.Add(vet);
        db.Clinics.Add(clinic);
        db.ClinicMembers.Add(new ClinicMember { ClinicId = clinic.Id, UserId = vet.Id });
        db.AvailabilitySlots.Add(slot);
        await db.SaveChangesAsync();
        return (client, petId, slot.Id, date);
    }

    private static string Url(Guid pet, DateOnly date) => $"/Appointments?petId={pet}&date={date:yyyy-MM-dd}&reason=Vacunas";

    private static async Task<HttpResponseMessage> Confirm(HttpClient client, Guid pet, Guid slot, DateOnly date)
    {
        var (token, _) = await WebTestHelpers.GetFormAsync(client, Url(pet, date));
        return await client.PostAsync("/Appointments/Confirm", WebTestHelpers.Form(token,
            ("PetId", pet.ToString()), ("SlotId", slot.ToString()), ("Date", date.ToString("yyyy-MM-dd")), ("Reason", "Vacunas")));
    }

    [Fact]
    public async Task Home_links_to_booking_and_booking_requires_login()
    {
        using var client = WebTestHelpers.CreateClient(_factory);
        var response = await client.GetAsync("/Appointments");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
        using var owner = await WebTestHelpers.RegisterAndLoginAsync(_factory);
        var html = await owner.GetStringAsync("/");
        Assert.Contains("/Appointments", html);
        Assert.Contains("Agendar Cita", html);
        var empty = await owner.GetStringAsync("/Appointments");
        Assert.Contains("Primero registra una mascota", empty);
    }

    [Fact]
    public async Task Owner_selects_pet_and_local_slot_then_receives_persisted_confirmation()
    {
        var (client, pet, slot, date) = await Prepare();
        using (client)
        {
            var html = await client.GetStringAsync(Url(pet, date));
            Assert.Contains($"value=\"{pet}\"", html);
            Assert.Contains($"value=\"{slot}\"", html);
            Assert.Contains("23:30", html);
            var response = await Confirm(client, pet, slot, date);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var summary = WebUtility.HtmlDecode(await client.GetStringAsync(response.Headers.Location));
            Assert.Contains("Cita guardada correctamente", summary);
            Assert.Contains("Luna", summary);
            Assert.Contains(date.ToString("dd/MM/yyyy"), summary);
            Assert.Contains("23:30", summary);
            Assert.Contains("Vacunas", summary);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var appointment = await db.Appointments.SingleAsync(a => a.SlotId == slot);
            Assert.Equal("Vacunas", appointment.Reason);
            Assert.True((await db.AvailabilitySlots.SingleAsync(s => s.Id == slot)).IsBooked);
            Assert.True(await db.Notifications.AnyAsync(n => n.RelatedEntityId == appointment.Id && n.RelatedEntityType == "AppointmentReminder"));
            using var stranger = await WebTestHelpers.RegisterAndLoginAsync(_factory);
            Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(response.Headers.Location)).StatusCode);
        }
    }

    [Fact]
    public async Task Occupied_slot_is_hidden_and_stale_confirmation_does_not_duplicate_booking()
    {
        var (client, pet, slot, date) = await Prepare();
        using (client)
        {
            Assert.Equal(HttpStatusCode.Redirect, (await Confirm(client, pet, slot, date)).StatusCode);
            var html = await client.GetStringAsync(Url(pet, date));
            Assert.DoesNotContain($"value=\"{slot}\"", html);
            var stale = await Confirm(client, pet, slot, date);
            Assert.Equal(HttpStatusCode.OK, stale.StatusCode);
            Assert.Contains("El horario ya no", WebUtility.HtmlDecode(await stale.Content.ReadAsStringAsync()));
            using var scope = _factory.Services.CreateScope();
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<AppDbContext>().Appointments.CountAsync(a => a.SlotId == slot));
        }
    }

    [Fact]
    public async Task Foreign_pet_invalid_slot_and_missing_antiforgery_are_rejected()
    {
        var (client, pet, slot, date) = await Prepare();
        using (client)
        using (var other = await WebTestHelpers.RegisterAndLoginAsync(_factory))
        {
            var response = await Confirm(other, pet, slot, date);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Selecciona una de tus mascotas", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
            Assert.Equal(HttpStatusCode.OK, (await Confirm(client, pet, Guid.NewGuid(), date)).StatusCode);
            var missingToken = await client.PostAsync("/Appointments/Confirm", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["PetId"] = pet.ToString(), ["SlotId"] = slot.ToString(), ["Date"] = date.ToString("yyyy-MM-dd"), ["Reason"] = "Vacunas" }));
            Assert.Equal(HttpStatusCode.BadRequest, missingToken.StatusCode);
            using var scope = _factory.Services.CreateScope();
            Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Appointments.AnyAsync(a => a.SlotId == slot));
        }
    }
}
