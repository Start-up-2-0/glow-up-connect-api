using GLOWAPI.Application.DTOs.Caixa;
using GLOWAPI.Application.DTOs.Financeiro;
using GLOWAPI.Domain.Enums;

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

    Task<ComissaoProfissionalResponseDto> CriarComissaoAsync(
        int estabelecimentoId,
        CriarComissaoProfissionalRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ComissaoProfissionalResponseDto> AtualizarComissaoAsync(
        int estabelecimentoId,
        int comissaoId,
        AtualizarComissaoProfissionalRequestDto request,
        CancellationToken cancellationToken = default);

    Task DesativarComissaoAsync(
        int estabelecimentoId,
        int comissaoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComissaoProfissionalExtratoDto>> ListarMinhasComissoesAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<RelatorioAnaliticoResponseDto> ObterRelatorioAnaliticoAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<FluxoCaixaResponseDto> ObterFluxoCaixaAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<string> ExportarRelatorioCsvAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<FinanceiroExportacaoResponseDto> ExportarRelatorioAsync(
        int estabelecimentoId,
        FinanceiroFiltroDto filtro,
        string formato,
        CancellationToken cancellationToken = default);

    Task<FinanceiroBuscaResponseDto> BuscarAsync(
        int estabelecimentoId,
        string termo,
        string? tipo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContaReceberResponseDto>> ListarContasReceberAsync(
        int estabelecimentoId,
        ContaFinanceiraStatus? status,
        CancellationToken cancellationToken = default);

    Task<ContaReceberResponseDto> CriarContaReceberAsync(
        int estabelecimentoId,
        CriarContaReceberRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ContaReceberResponseDto> BaixarContaReceberAsync(
        int estabelecimentoId,
        int contaId,
        BaixarContaRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ContaReceberResponseDto> CancelarContaReceberAsync(
        int estabelecimentoId,
        int contaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContaPagarResponseDto>> ListarContasPagarAsync(
        int estabelecimentoId,
        ContaFinanceiraStatus? status,
        CancellationToken cancellationToken = default);

    Task<ContaPagarResponseDto> CriarContaPagarAsync(
        int estabelecimentoId,
        CriarContaPagarRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ContaPagarResponseDto> BaixarContaPagarAsync(
        int estabelecimentoId,
        int contaId,
        BaixarContaRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ContaPagarResponseDto> CancelarContaPagarAsync(
        int estabelecimentoId,
        int contaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConciliacaoItemResponseDto>> ListarConciliacaoAsync(
        int estabelecimentoId,
        bool? conciliado,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConciliacaoItemResponseDto>> ImportarConciliacaoCsvAsync(
        int estabelecimentoId,
        ConciliacaoImportacaoRequestDto request,
        CancellationToken cancellationToken = default);

    Task AtualizarContasVencidasAsync(CancellationToken cancellationToken = default);
}
