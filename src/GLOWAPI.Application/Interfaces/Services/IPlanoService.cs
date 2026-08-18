using GLOWAPI.Application.DTOs.Planos;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IPlanoService
{
    Task<PlanosAtivosResponseDto> ListarAtivosAsync(
        TipoAssinatura tipoAssinatura = TipoAssinatura.Estabelecimento,
        CancellationToken cancellationToken = default);
}
