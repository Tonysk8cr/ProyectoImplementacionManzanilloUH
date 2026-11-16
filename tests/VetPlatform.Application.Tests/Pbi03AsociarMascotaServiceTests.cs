using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Clinical;
using VetPlatform.Domain.Entities.Audit;
using VetPlatform.Domain.Enums;
using VetPlatform.Infrastructure.Data;
using Xunit;

namespace VetPlatform.Application.Tests;

/// <summary>
/// PBI-03: Como propietario, quiero asociar mi mascota con mi perfil para reconocer que forma parte de mis mascotas.
/// Tarea "Comprobar asociación": verifica que cada mascota queda vinculada a su propietario
/// y que ningún otro propietario puede verla ni registrarla a su nombre.
/// </summary>
public class Pbi03AsociarMascotaServiceTests : IDisposable
{
    private readonly TestFixture _f = new();
    public void Dispose() => _f.Dispose();
    private static readonly UserRole[] Owner = { UserRole.PetOwner };

    private Task<Result<PetDto>> CrearAsync(Guid actor, Guid ownerId, string name, string species, UserRole[]? roles = null) =>
        _f.RunAs<IClinicalService, Result<PetDto>>(actor, roles ?? Owner, svc =>
            svc.CreatePetAsync(new CreatePetCommand(ownerId, name, species, null, null)));

    private Task<Result<IReadOnlyList<PetDto>>> MisMascotasAsync(Guid ownerId) =>
        _f.RunAs<IClinicalService, Result<IReadOnlyList<PetDto>>>(ownerId, Owner, svc => svc.GetMyPetsAsync());

    private async Task<int> ContarMascotasAsync(Guid ownerId)
    {
        using var scope = _f.NewScope();
        return await _f.Get<AppDbContext>(scope).Pets.CountAsync(p => p.OwnerId == ownerId);
    }

    [Fact]
    public async Task Mascota_queda_vinculada_al_propietario_que_la_registra()
    {
        var owner = await _f.SeedUserAsync(UserRole.PetOwner);

        var r = await CrearAsync(owner, owner, "Firulais", "Perro");

        r.Success.Should().BeTrue(string.Join(",", r.Errors));
        r.Value!.OwnerId.Should().Be(owner);

        using var scope = _f.NewScope();
        var guardada = await _f.Get<AppDbContext>(scope).Pets.SingleAsync(p => p.Id == r.Value.Id);
        guardada.OwnerId.Should().Be(owner);
    }

    [Fact]
    public async Task No_se_puede_registrar_una_mascota_a_nombre_de_otro_propietario()
    {
        var ownerA = await _f.SeedUserAsync(UserRole.PetOwner);
        var ownerB = await _f.SeedUserAsync(UserRole.PetOwner);

        // A está conectado, pero intenta registrar la mascota a nombre de B.
        var r = await CrearAsync(ownerA, ownerB, "Intrusa", "Gato");

        r.Success.Should().BeFalse();
        r.ErrorType.Should().Be(ErrorType.Forbidden);
        (await ContarMascotasAsync(ownerB)).Should().Be(0);
        (await ContarMascotasAsync(ownerA)).Should().Be(0);
    }

    [Fact]
    public async Task Usuario_que_no_es_propietario_no_puede_registrar_mascotas()
    {
        var vet = await _f.SeedUserAsync(UserRole.AttendingVet);

        var r = await CrearAsync(vet, vet, "Firulais", "Perro", new[] { UserRole.AttendingVet });

        r.Success.Should().BeFalse();
        r.ErrorType.Should().Be(ErrorType.Forbidden);
        (await ContarMascotasAsync(vet)).Should().Be(0);
    }

    [Fact]
    public async Task Mis_mascotas_solo_muestra_las_del_propietario_conectado()
    {
        var ownerA = await _f.SeedUserAsync(UserRole.PetOwner);
        var ownerB = await _f.SeedUserAsync(UserRole.PetOwner);
        await CrearAsync(ownerA, ownerA, "Rocky", "Perro");
        await CrearAsync(ownerB, ownerB, "Michi", "Gato");

        var listaA = await MisMascotasAsync(ownerA);
        var listaB = await MisMascotasAsync(ownerB);

        listaA.Success.Should().BeTrue(string.Join(",", listaA.Errors));
        listaA.Value!.Should().ContainSingle(p => p.Name == "Rocky");
        listaA.Value.Should().NotContain(p => p.Name == "Michi");
        listaA.Value.Should().OnlyContain(p => p.OwnerId == ownerA);

        listaB.Success.Should().BeTrue(string.Join(",", listaB.Errors));
        listaB.Value!.Should().ContainSingle(p => p.Name == "Michi");
        listaB.Value.Should().NotContain(p => p.Name == "Rocky");
        listaB.Value.Should().OnlyContain(p => p.OwnerId == ownerB);
    }

    [Fact]
    public async Task Propietario_puede_ver_el_detalle_de_su_mascota()
    {
        var owner = await _f.SeedUserAsync(UserRole.PetOwner);
        var creada = await CrearAsync(owner, owner, "Luna", "Gato");

        var r = await _f.RunAs<IClinicalService, Result<PetDto>>(owner, Owner, svc => svc.GetPetAsync(creada.Value!.Id));

        r.Success.Should().BeTrue(string.Join(",", r.Errors));
        r.Value!.Name.Should().Be("Luna");
        r.Value.OwnerId.Should().Be(owner);
    }

    [Fact]
    public async Task Otro_propietario_no_puede_ver_el_detalle_de_una_mascota_ajena()
    {
        var ownerA = await _f.SeedUserAsync(UserRole.PetOwner);
        var ownerB = await _f.SeedUserAsync(UserRole.PetOwner);
        var mascotaDeA = await CrearAsync(ownerA, ownerA, "Luna", "Gato");

        var r = await _f.RunAs<IClinicalService, Result<PetDto>>(ownerB, Owner, svc => svc.GetPetAsync(mascotaDeA.Value!.Id));

        r.Success.Should().BeFalse();
        r.ErrorType.Should().Be(ErrorType.Forbidden);
        r.Value.Should().BeNull();
    }

    [Fact]
    public async Task Otro_propietario_no_puede_ver_el_historial_de_una_mascota_ajena()
    {
        var ownerA = await _f.SeedUserAsync(UserRole.PetOwner);
        var ownerB = await _f.SeedUserAsync(UserRole.PetOwner);
        var mascotaDeA = await CrearAsync(ownerA, ownerA, "Luna", "Gato");

        var r = await _f.RunAs<IClinicalService, Result<ClinicalRecordDto>>(ownerB, Owner, svc => svc.GetHistoryAsync(mascotaDeA.Value!.Id));

        r.Success.Should().BeFalse();
        r.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Acceso_denegado_a_mascota_ajena_queda_auditado()
    {
        var ownerA = await _f.SeedUserAsync(UserRole.PetOwner);
        var ownerB = await _f.SeedUserAsync(UserRole.PetOwner);
        var mascotaDeA = await CrearAsync(ownerA, ownerA, "Luna", "Gato");

        await _f.RunAs<IClinicalService, Result<PetDto>>(ownerB, Owner, svc => svc.GetPetAsync(mascotaDeA.Value!.Id));

        using var scope = _f.NewScope();
        (await _f.Get<AppDbContext>(scope).AuditLogs
            .CountAsync(a => a.ActorUserId == ownerB && a.Result == AuditLog.Results.Denied))
            .Should().BeGreaterThan(0);
    }
}