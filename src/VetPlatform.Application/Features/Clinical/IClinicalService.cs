using VetPlatform.Application.Common;
namespace VetPlatform.Application.Features.Clinical;

public interface IClinicalService
{
    Task<Result<PetDto>> CreatePetAsync(CreatePetCommand cmd, CancellationToken ct = default);
    /// <summary>Pets owned by the authenticated user (Sprint 1: owner's pet list).</summary>
    Task<Result<IReadOnlyList<PetDto>>> GetMyPetsAsync(CancellationToken ct = default);
    /// <summary>Pets with an active attending-vet relationship to the authenticated vet (patient list).</summary>
    Task<Result<IReadOnlyList<PetDto>>> GetMyPatientsAsync(CancellationToken ct = default);
    Task<Result<PetDto>> GetPetAsync(Guid petId, CancellationToken ct = default);
    Task<Result<ClinicalRecordDto>> GetHistoryAsync(Guid petId, CancellationToken ct = default);
    Task<Result<ConsultationDto>> RegisterConsultationAsync(RegisterConsultationCommand cmd, CancellationToken ct = default);
    Task<Result<ConsultationDto>> GetConsultationAsync(Guid id, CancellationToken ct = default);
    /// <summary>Consultation detail scoped to a patient: NotFound if it belongs to another pet.</summary>
    Task<Result<ConsultationDto>> GetPetConsultationAsync(Guid petId, Guid consultationId, CancellationToken ct = default);
    Task<Result<AddendumDto>> AddAddendumAsync(AddAddendumCommand cmd, CancellationToken ct = default);
    Task<Result<PrescriptionDto>> RegisterPrescriptionAsync(RegisterPrescriptionCommand cmd, CancellationToken ct = default);
    Task<Result<PrescriptionDto>> GetPrescriptionAsync(Guid id, CancellationToken ct = default);
    Task<Result<AddendumDto>> AddPrescriptionAddendumAsync(AddPrescriptionAddendumCommand cmd, CancellationToken ct = default);
}
