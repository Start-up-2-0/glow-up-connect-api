using GLOWAPI.Application.DTOs.Caixa;
using GLOWAPI.Application.DTOs.Financeiro;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IFinanceiroNegocioService
{
    Task<FinanceiroResumoResponseDto> ObterResumoAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LancamentoCaixaResponseDto>> ListarRelatorioAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComissaoProfissionalResponseDto>> ListarComissoesAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);
}
