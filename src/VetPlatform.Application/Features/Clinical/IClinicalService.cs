using VetPlatform.Application.Common;
namespace VetPlatform.Application.Features.Clinical;

public interface IClinicalService
{
    Task<Result<PetDto>> CreatePetAsync(CreatePetCommand cmd, CancellationToken ct = default);
    Task<Result<PetDto>> GetPetAsync(Guid petId, CancellationToken ct = default);
    Task<Result<ClinicalRecordDto>> GetHistoryAsync(Guid petId, CancellationToken ct = default);
    Task<Result<ConsultationDto>> RegisterConsultationAsync(RegisterConsultationCommand cmd, CancellationToken ct = default);
    Task<Result<ConsultationDto>> GetConsultationAsync(Guid id, CancellationToken ct = default);
    Task<Result<AddendumDto>> AddAddendumAsync(AddAddendumCommand cmd, CancellationToken ct = default);
    Task<Result<PrescriptionDto>> RegisterPrescriptionAsync(RegisterPrescriptionCommand cmd, CancellationToken ct = default);
    Task<Result<PrescriptionDto>> GetPrescriptionAsync(Guid id, CancellationToken ct = default);
    Task<Result<AddendumDto>> AddPrescriptionAddendumAsync(AddPrescriptionAddendumCommand cmd, CancellationToken ct = default);
}
