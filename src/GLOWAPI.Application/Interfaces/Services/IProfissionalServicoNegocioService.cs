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

    Task<ProfissionalServicoResponseDto> AtualizarAsync(
        int estabelecimentoId,
        int profissionalId,
        int servicoId,
        AtualizarProfissionalServicoRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ProfissionalServicoResponseDto> DesvincularAsync(
        int estabelecimentoId,
        int profissionalId,
        int servicoId,
        CancellationToken cancellationToken = default);
}
