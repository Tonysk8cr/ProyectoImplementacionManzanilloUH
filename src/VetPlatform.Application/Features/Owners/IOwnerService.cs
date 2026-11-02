using VetPlatform.Application.Common;
using VetPlatform.Application.Features.Identity;

namespace VetPlatform.Application.Features.Owners;

/// <summary>CRUD del perfil del propietario autenticado (PBI-01). Siempre opera sobre el perfil de quien tiene la sesión.</summary>
public interface IOwnerService
{
    /// <summary>Registro público del propietario (PBI-01): cuenta + cédula, teléfono y dirección.</summary>
    Task<Result<UserDto>> RegisterOwnerAsync(RegisterOwnerCommand cmd, CancellationToken ct = default);

    Task<Result<OwnerProfileDto>> GetMyProfileAsync(CancellationToken ct = default);
    Task<Result<OwnerProfileDto>> CreateMyProfileAsync(CreateOwnerProfileCommand cmd, CancellationToken ct = default);
    Task<Result<OwnerProfileDto>> UpdateMyProfileAsync(UpdateOwnerProfileCommand cmd, CancellationToken ct = default);
    Task<Result<bool>> DeleteMyProfileAsync(CancellationToken ct = default);
}
