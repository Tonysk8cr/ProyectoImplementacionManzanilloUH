using VetPlatform.Application.Features.Clinical;
using VetPlatform.Application.Features.Organizations;
using VetPlatform.Domain.Enums;

namespace VetPlatform.Application.Tests;

/// <summary>Builds a common clinical scenario: owner + pet + clinic with an active regente.</summary>
public class ClinicalScenario
{
    public Guid AdminId, OwnerId, VetId, ClinicId, PetId, RecordId, QrCode_Unused;
    public PetDto Pet = default!;

    public static async Task<ClinicalScenario> CreateAsync(TestFixture f, bool assignVet = true)
    {
        var s = new ClinicalScenario();
        s.AdminId = await f.SeedUserAsync(UserRole.PlatformAdmin);
        s.OwnerId = await f.SeedUserAsync(UserRole.PetOwner);
        s.VetId = await f.SeedUserAsync(UserRole.AttendingVet);
        var clinic = await f.RunAs<IClinicService, Common.Result<ClinicDto>>(s.AdminId, new[] { UserRole.PlatformAdmin },
            svc => svc.CreateClinicAsync(new CreateClinicCommand("Clínica Central", s.AdminId, "Calle 1", "555")));
        s.ClinicId = clinic.Value!.Id;
        await f.RunAs<IClinicService, Common.Result<ClinicMemberDto>>(s.AdminId, new[] { UserRole.PlatformAdmin },
            svc => svc.AddMemberAsync(new AddMemberCommand(s.ClinicId, s.VetId)));
        var pet = await f.RunAs<IClinicalService, Common.Result<PetDto>>(s.OwnerId, new[] { UserRole.PetOwner },
            svc => svc.CreatePetAsync(new CreatePetCommand(s.OwnerId, "Firulais", "Perro", "Mestizo", null)));
        s.Pet = pet.Value!;
        s.PetId = pet.Value!.Id;
        var hist = await f.RunAs<IClinicalService, Common.Result<ClinicalRecordDto>>(s.OwnerId, new[] { UserRole.PetOwner }, svc => svc.GetHistoryAsync(s.PetId));
        s.RecordId = hist.Value!.Id;
        if (assignVet)
            await f.RunAs<IVetRelationshipService, Common.Result<AttendingVetRelationshipDto>>(s.OwnerId, new[] { UserRole.PetOwner },
                svc => svc.AssignAsync(new AssignAttendingVetCommand(s.PetId, s.VetId, s.ClinicId)));
        return s;
    }
}
