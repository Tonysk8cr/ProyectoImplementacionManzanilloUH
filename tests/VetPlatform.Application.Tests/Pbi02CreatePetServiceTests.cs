using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Data;
using Xunit;

namespace VetPlatform.Application.Tests;

/// <summary>
/// PBI-02: Como propietario, quiero registrar una mascota para mantener su información básica.
/// Pruebas a nivel de servicio (sin pasar por el login web).
/// </summary>
public class Pbi02CreatePetServiceTests : IDisposable
{
    private readonly TestFixture _f = new();
    public void Dispose() => _f.Dispose();
    private static readonly UserRole[] Owner = { UserRole.PetOwner };

    private Task<Result<PetDto>> CrearAsync(Guid ownerId, string name, string species, string? breed = null, DateOnly? birthDate = null) =>
        _f.RunAs<IClinicalService, Result<PetDto>>(ownerId, Owner, svc =>
            svc.CreatePetAsync(new CreatePetCommand(ownerId, name, species, breed, birthDate)));

    private async Task<int> ContarMascotasAsync(Guid ownerId)
    {
        using var scope = _f.NewScope();
        return await _f.Get<AppDbContext>(scope).Pets.CountAsync(p => p.OwnerId == ownerId);
    }

    [Fact]
    public async Task Registro_valido_guarda_los_datos_y_asigna_el_dueno()
    {
        var owner = await _f.SeedUserAsync(UserRole.PetOwner);
        var nacimiento = new DateOnly(2022, 3, 15);

        var r = await CrearAsync(owner, "Firulais", "Perro", "Labrador", nacimiento);

        r.Success.Should().BeTrue(string.Join(",", r.Errors));
        r.Value!.Name.Should().Be("Firulais");
        r.Value.Species.Should().Be("Perro");
        r.Value.Breed.Should().Be("Labrador");
        r.Value.BirthDate.Should().Be(nacimiento);
        r.Value.OwnerId.Should().Be(owner);
        r.Value.QrCode.Should().NotBeNullOrEmpty()
            .And.NotBe(r.Value.Id.ToString())
            .And.NotBe(r.Value.Id.ToString("N"));
        (await ContarMascotasAsync(owner)).Should().Be(1);
    }

    [Fact]
    public async Task Raza_y_fecha_son_opcionales()
    {
        var owner = await _f.SeedUserAsync(UserRole.PetOwner);

        var r = await CrearAsync(owner, "Michi", "Gato");

        r.Success.Should().BeTrue(string.Join(",", r.Errors));
        r.Value!.Breed.Should().BeNull();
        r.Value.BirthDate.Should().BeNull();
    }

    [Fact]
    public async Task Sin_nombre_no_se_guarda()
    {
        var owner = await _f.SeedUserAsync(UserRole.PetOwner);

        var r = await CrearAsync(owner, "", "Perro");

        r.Success.Should().BeFalse();
        r.Errors.Should().NotBeEmpty();
        (await ContarMascotasAsync(owner)).Should().Be(0);
    }

    [Fact]
    public async Task Sin_especie_no_se_guarda()
    {
        var owner = await _f.SeedUserAsync(UserRole.PetOwner);

        var r = await CrearAsync(owner, "Firulais", "");

        r.Success.Should().BeFalse();
        r.Errors.Should().NotBeEmpty();
        (await ContarMascotasAsync(owner)).Should().Be(0);
    }

    [Fact]
    public async Task Fecha_de_nacimiento_futura_no_se_guarda()
    {
        var owner = await _f.SeedUserAsync(UserRole.PetOwner);
        var manana = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var r = await CrearAsync(owner, "Firulais", "Perro", birthDate: manana);

        r.Success.Should().BeFalse();
        (await ContarMascotasAsync(owner)).Should().Be(0);
    }

    [Fact]
    public async Task Fecha_de_nacimiento_de_hace_mas_de_40_anios_no_se_guarda()
    {
        var owner = await _f.SeedUserAsync(UserRole.PetOwner);
        var hace41Anios = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-41);

        var r = await CrearAsync(owner, "Firulais", "Perro", birthDate: hace41Anios);

        r.Success.Should().BeFalse();
        (await ContarMascotasAsync(owner)).Should().Be(0);
    }

    [Fact]
    public async Task Espacios_extra_se_limpian_y_raza_en_blanco_queda_vacia()
    {
        var owner = await _f.SeedUserAsync(UserRole.PetOwner);

        var r = await CrearAsync(owner, "  Luna  ", "  Gato  ", "   ");

        r.Success.Should().BeTrue(string.Join(",", r.Errors));
        r.Value!.Name.Should().Be("Luna");
        r.Value.Species.Should().Be("Gato");
        r.Value.Breed.Should().BeNull();
    }

    [Fact]
    public async Task Mascota_registrada_aparece_en_mis_mascotas()
    {
        var owner = await _f.SeedUserAsync(UserRole.PetOwner);
        await CrearAsync(owner, "Rocky", "Perro");

        var lista = await _f.RunAs<IClinicalService, Result<IReadOnlyList<PetDto>>>(owner, Owner, svc => svc.GetMyPetsAsync());

        lista.Success.Should().BeTrue(string.Join(",", lista.Errors));
        lista.Value!.Should().ContainSingle(p => p.Name == "Rocky");
    }

    [Fact]
    public async Task Cada_mascota_recibe_un_qr_distinto()
    {
        var owner = await _f.SeedUserAsync(UserRole.PetOwner);

        var a = await CrearAsync(owner, "Uno", "Perro");
        var b = await CrearAsync(owner, "Dos", "Gato");

        a.Value!.QrCode.Should().NotBe(b.Value!.QrCode);
    }
}