using GLOWAPI.Application.DTOs.Caixa;

namespace GLOWAPI.Application.Interfaces.Services;

public interface ICaixaNegocioService
{
    Task<CaixaResumoResponseDto> ObterResumoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LancamentoCaixaResponseDto>> ListarLancamentosAsync(
        int estabelecimentoId,
        LancamentoCaixaFiltroDto filtro,
        CancellationToken cancellationToken = default);
}
