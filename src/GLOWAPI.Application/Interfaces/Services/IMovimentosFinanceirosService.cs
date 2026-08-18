using GLOWAPI.Application.DTOs.Financeiro;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IMovimentosFinanceirosService
{
    Task<FinanceiroDashboardResponseDto> ObterDashboardAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<MovimentosFinanceirosPaginadoResponseDto> ListarEntradasAsync(
        int estabelecimentoId,
        MovimentosFinanceirosFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<MovimentosFinanceirosPaginadoResponseDto> ListarSaidasAsync(
        int estabelecimentoId,
        MovimentosFinanceirosFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<MovimentoFinanceiroResponseDto> CriarEntradaAsync(
        int estabelecimentoId,
        CriarMovimentoFinanceiroRequestDto request,
        CancellationToken cancellationToken = default);

    Task<MovimentoFinanceiroResponseDto> CriarSaidaAsync(
        int estabelecimentoId,
        CriarMovimentoFinanceiroRequestDto request,
        CancellationToken cancellationToken = default);

    Task<MovimentoFinanceiroResponseDto> MarcarEntradaRecebidaAsync(
        int estabelecimentoId,
        string movimentoId,
        BaixarContaRequestDto? request,
        CancellationToken cancellationToken = default);

    Task<MovimentoFinanceiroResponseDto> MarcarSaidaPagaAsync(
        int estabelecimentoId,
        string movimentoId,
        BaixarContaRequestDto? request,
        CancellationToken cancellationToken = default);
}
