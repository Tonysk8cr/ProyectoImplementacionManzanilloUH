using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VetPlatform.Domain.Entities.Clinical;
using VetPlatform.Domain.Entities.Identity;
using VetPlatform.Domain.Entities.Organizations;
using VetPlatform.Domain.Entities.Scheduling;
using static VetPlatform.Web.Tests.WebTestHelpers;

namespace VetPlatform.Web.Tests;

public sealed class AppointmentBookingFlowTests : IClassFixture<VetPlatformWebFactory>
{
    private readonly VetPlatformWebFactory _factory;

    public AppointmentBookingFlowTests(VetPlatformWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Owner_books_and_sees_appointment_and_stale_confirmation_refreshes_availability()
    {
        using var client = CreateClient(_factory);
        var email = $"appointment{Guid.NewGuid():N}@test.com";
        const string password = "Secreta1";

        var (registerToken, _) = await GetFormAsync(client, "/Account/Register");
        await client.PostAsync("/Account/Register", Form(registerToken,
            ("FullName", "Dueño de prueba"),
            ("Email", email),
            ("IdentificationNumber", Guid.NewGuid().ToString("N")[..10]),
            ("Phone", "88888888"),
            ("Password", password),
            ("ConfirmPassword", password)));
        var (loginToken, _) = await GetFormAsync(client, "/Account/Login");
        var login = await client.PostAsync("/Account/Login", Form(loginToken,
            ("Email", email), ("Password", password)));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        Guid clinicId;
        Guid petId;
        Guid slotId;
        DateOnly date;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VetPlatform.Infrastructure.Data.AppDbContext>();
            var owner = await db.Users.SingleAsync(u => u.Email == email.ToLowerInvariant());
            var vet = new User
            {
                Email = $"vet{Guid.NewGuid():N}@test.com",
                FullName = "Veterinaria de prueba",
                PasswordHash = "not-used",
                IsVerified = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var clinic = new Clinic
            {
                Name = "Clínica de prueba",
                RepresentativeUserId = vet.Id,
                Address = "Centro",
                Phone = "555",
                CreatedAt = DateTimeOffset.UtcNow
            };
            var pet = new Pet
            {
                OwnerId = owner.Id,
                Name = "Nube",
                Species = "Gato",
                QrCode = Pet.GenerateOpaqueQrCode(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            var start = DateTimeOffset.UtcNow.AddDays(2).AddHours(1);
            var slot = new AvailabilitySlot
            {
                ClinicId = clinic.Id,
                VetUserId = vet.Id,
                StartTime = start,
                EndTime = start.AddMinutes(30)
            };
            db.AddRange(vet, clinic, pet, slot, new ClinicMember
            {
                ClinicId = clinic.Id,
                UserId = vet.Id,
                JoinedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
            clinicId = clinic.Id;
            petId = pet.Id;
            slotId = slot.Id;
            date = DateOnly.FromDateTime(start.UtcDateTime);
        }

        var bookingPage = await client.GetAsync(
            $"/Appointments/Book?clinicId={clinicId}&petId={petId}&date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, bookingPage.StatusCode);
        var bookingHtml = WebUtility.HtmlDecode(await bookingPage.Content.ReadAsStringAsync());
        Assert.Contains("Clínica de prueba", bookingHtml);
        Assert.Contains(slotId.ToString(), bookingHtml);
        var token = AntiforgeryToken(bookingHtml);

        var confirmation = await client.PostAsync("/Appointments/Book", Form(token,
            ("ClinicId", clinicId.ToString()),
            ("PetId", petId.ToString()),
            ("Date", date.ToString("yyyy-MM-dd")),
            ("SlotId", slotId.ToString()),
            ("Reason", "Vacunación anual")));
        Assert.Equal(HttpStatusCode.Redirect, confirmation.StatusCode);
        Assert.Contains("/Appointments/Confirmation", confirmation.Headers.Location!.ToString());

        var summary = await client.GetAsync(confirmation.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        var summaryHtml = WebUtility.HtmlDecode(await summary.Content.ReadAsStringAsync());
        Assert.Contains("Clínica de prueba", summaryHtml);
        Assert.Contains("Nube", summaryHtml);
        Assert.Contains("Vacunación anual", summaryHtml);
        Assert.Contains("Booked", summaryHtml);

        var appointments = await client.GetAsync("/Appointments");
        Assert.Equal(HttpStatusCode.OK, appointments.StatusCode);
        var appointmentsHtml = WebUtility.HtmlDecode(await appointments.Content.ReadAsStringAsync());
        Assert.Contains("Clínica de prueba", appointmentsHtml);
        Assert.Contains("Vacunación anual", appointmentsHtml);

        var staleConfirmation = await client.PostAsync("/Appointments/Book", Form(token,
            ("ClinicId", clinicId.ToString()),
            ("PetId", petId.ToString()),
            ("Date", date.ToString("yyyy-MM-dd")),
            ("SlotId", slotId.ToString()),
            ("Reason", "Vacunación anual")));
        Assert.Equal(HttpStatusCode.OK, staleConfirmation.StatusCode);
        var conflictHtml = WebUtility.HtmlDecode(await staleConfirmation.Content.ReadAsStringAsync());
        Assert.Contains("ya no está disponible", conflictHtml);
        Assert.DoesNotContain($"<option value=\"{slotId}\"", conflictHtml);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<VetPlatform.Infrastructure.Data.AppDbContext>();
        Assert.Equal(1, await verificationDb.Appointments.CountAsync(a => a.SlotId == slotId));
    }
}
