using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VetPlatform.Domain.Entities.Clinical;
using VetPlatform.Domain.Entities.Identity;
using VetPlatform.Domain.Entities.Organizations;
using VetPlatform.Domain.Entities.Scheduling;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Data;
using VetPlatform.Infrastructure.Security;

namespace VetPlatform.Web.Tests;

public class ClinicAppointmentsWebTests : IClassFixture<VetPlatformWebFactory>
{
    private readonly VetPlatformWebFactory _factory;
    public ClinicAppointmentsWebTests(VetPlatformWebFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, Guid User)> Login(UserRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = new User { Email = $"qa-{Guid.NewGuid():N}@test.com", FullName = "Personal QA", IsVerified = true,
            PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash("Secreta1") };
        user.UserRoles.Add(new UserRoleAssignment { UserId = user.Id, Role = role });
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var client = WebTestHelpers.CreateClient(_factory);
        var (token, _) = await WebTestHelpers.GetFormAsync(client, "/Account/Login");
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Account/Login", WebTestHelpers.Form(token,
            ("Email", user.Email), ("Password", "Secreta1")))).StatusCode);
        return (client, user.Id);
    }

    private async Task<(Guid Clinic, Guid Appointment)> Seed(Guid vet)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var owner = new User { Email = $"owner-{Guid.NewGuid():N}@test.com", FullName = "Cliente QA", PasswordHash = "x" };
        var clinic = new Clinic { Name = "Clínica QA", RepresentativeUserId = vet };
        var pet = new Pet { Name = "Luna QA", Species = "Gato", OwnerId = owner.Id, QrCode = Pet.GenerateOpaqueQrCode() };
        var slot = new AvailabilitySlot { ClinicId = clinic.Id, VetUserId = vet, StartTime = DateTimeOffset.UtcNow.AddDays(-1), EndTime = DateTimeOffset.UtcNow.AddDays(-1).AddMinutes(30), IsBooked = true };
        var appointment = new Appointment { ClinicId = clinic.Id, VetUserId = vet, OwnerId = owner.Id, PetId = pet.Id, SlotId = slot.Id, Reason = "Vacunas QA", CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) };
        db.Users.Add(owner); db.Clinics.Add(clinic); db.Pets.Add(pet); db.AvailabilitySlots.Add(slot); db.Appointments.Add(appointment);
        db.ClinicMembers.Add(new ClinicMember { ClinicId = clinic.Id, UserId = vet });
        await db.SaveChangesAsync();
        return (clinic.Id, appointment.Id);
    }

    [Fact]
    public async Task Staff_can_filter_read_update_and_observe_persisted_state_in_both_views()
    {
        var (client, user) = await Login(UserRole.AttendingVet);
        using (client)
        {
            var (clinic, id) = await Seed(user);
            var listUrl = $"/ClinicAppointments?clinicId={clinic}&allDates=true&status=Booked";
            var listing = WebUtility.HtmlDecode(await client.GetStringAsync(listUrl));
            Assert.Contains("Luna QA", listing); Assert.Contains("Cliente QA", listing); Assert.Contains("Vacunas QA", listing);
            Assert.Contains("Marcar completada", listing);
            var detailsUrl = $"/ClinicAppointments/Details/{id}";
            var (token, _) = await WebTestHelpers.GetFormAsync(client, detailsUrl);
            var changed = await client.PostAsync("/ClinicAppointments/ChangeStatus", WebTestHelpers.Form(token, ("id", id.ToString()), ("status", "Completed")));
            Assert.Equal(HttpStatusCode.Redirect, changed.StatusCode);
            var details = WebUtility.HtmlDecode(await client.GetStringAsync(detailsUrl));
            Assert.Contains("Completada", details); Assert.DoesNotContain("Marcar completada", details);
            Assert.DoesNotContain("Luna QA", await client.GetStringAsync(listUrl));
            Assert.Contains("Luna QA", await client.GetStringAsync($"/ClinicAppointments?clinicId={clinic}&allDates=true&status=Completed"));
            using var scope = _factory.Services.CreateScope();
            Assert.Equal("Completed", (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Appointments.SingleAsync(a => a.Id == id)).Status);
        }
    }

    [Fact]
    public async Task Authentication_clinic_scope_and_antiforgery_protect_management()
    {
        using var anonymous = WebTestHelpers.CreateClient(_factory);
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync("/ClinicAppointments")).StatusCode);
        var (staff, user) = await Login(UserRole.AttendingVet);
        using (staff)
        {
            var (clinic, id) = await Seed(user);
            var (stranger, _) = await Login(UserRole.AttendingVet);
            using (stranger)
            {
                Assert.Equal(HttpStatusCode.Redirect, (await stranger.GetAsync($"/ClinicAppointments?clinicId={clinic}")).StatusCode); // Cookie middleware redirects Forbid to AccessDenied.
                Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/ClinicAppointments/Details/{id}")).StatusCode);
                var (token, _) = await WebTestHelpers.GetFormAsync(stranger, "/");
                Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync("/ClinicAppointments/ChangeStatus", WebTestHelpers.Form(token,
                    ("id", id.ToString()), ("status", "Cancelled")))).StatusCode);
            }
            var missingToken = await staff.PostAsync("/ClinicAppointments/ChangeStatus", new FormUrlEncodedContent(new Dictionary<string, string>
                { ["id"] = id.ToString(), ["status"] = "Cancelled" }));
            Assert.Equal(HttpStatusCode.BadRequest, missingToken.StatusCode);
            var (owner, _) = await Login(UserRole.PetOwner);
            using (owner)
            {
                var denied = await owner.GetAsync("/ClinicAppointments");
                Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
                Assert.Contains("AccessDenied", denied.Headers.Location!.ToString());
            }
        }
    }
}
