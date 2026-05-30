using GLOWAPI.Application.DTOs.Equipe;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IProfissionalServicoNegocioService
{
    Task<ProfissionalServicoResponseDto> VincularAsync(
        int estabelecimentoId,
        int profissionalId,
        int servicoId,
        VincularServicoProfissionalRequestDto request,
        CancellationToken cancellationToken = default);
}
