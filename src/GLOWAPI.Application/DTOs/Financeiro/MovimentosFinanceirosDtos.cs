namespace GLOWAPI.Application.DTOs.Financeiro;

public record MovimentoFinanceiroAcoesDto(
    bool PodeMarcarRecebido,
    bool PodeMarcarPago,
    bool PodeEstornar);

public record MovimentoFinanceiroResponseDto(
    string Id,
    string Direcao,
    decimal Valor,
    string Descricao,
    DateTime Data,
    string Status,
    string Origem,
    string? FormaPagamento,
    string? Categoria,
    DateTime? Vencimento,
    int? AgendamentoId,
    MovimentoFinanceiroAcoesDto Acoes);

public record MovimentosFinanceirosPaginadoResponseDto(
    int Total,
    int Pagina,
    int TamanhoPagina,
    IReadOnlyList<MovimentoFinanceiroResponseDto> Itens);

public record MovimentosFinanceirosFiltroDto(
    DateTime? Inicio = null,
    DateTime? Fim = null,
    string? Status = null,
    string? Q = null,
    int Pagina = 1,
    int TamanhoPagina = 20);

public record CriarMovimentoFinanceiroRequestDto(
    decimal Valor,
    string Descricao,
    DateTime? Data,
    string? FormaPagamento,
    string? Categoria,
    DateTime? Vencimento);

public record FinanceiroDashboardResponseDto(
    decimal SaldoAtual,
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal LucroLiquido,
    decimal ContasEmAberto,
    int QuantidadeContasEmAberto,
    decimal ComissoesPeriodo,
    DateTime? PeriodoInicio,
    DateTime? PeriodoFim);
