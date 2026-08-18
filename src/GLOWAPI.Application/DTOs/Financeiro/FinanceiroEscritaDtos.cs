using GLOWAPI.Application.DTOs.Caixa;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Financeiro;

public record CriarComissaoProfissionalRequestDto(
    int ProfissionalEstabelecimentoId,
    TipoComissao TipoComissao,
    decimal? Percentual,
    decimal? ValorFixo,
    DateTime InicioVigencia,
    DateTime? FimVigencia);

public record AtualizarComissaoProfissionalRequestDto(
    TipoComissao TipoComissao,
    decimal? Percentual,
    decimal? ValorFixo,
    DateTime InicioVigencia,
    DateTime? FimVigencia,
    bool Ativo);

public record ComissaoProfissionalExtratoDto(
    int LancamentoId,
    int? AgendamentoId,
    decimal Valor,
    string Descricao,
    DateTime CriadoEm);

public record RelatorioAnaliticoResponseDto(
    decimal FaturamentoTotal,
    int AtendimentosPagos,
    decimal TicketMedio,
    IReadOnlyList<RelatorioPorProfissionalDto> PorProfissional,
    IReadOnlyList<RelatorioPorFormaPagamentoDto> PorFormaPagamento);

public record RelatorioPorProfissionalDto(
    int ProfissionalId,
    string NomePublico,
    decimal Faturamento,
    int Quantidade);

public record RelatorioPorFormaPagamentoDto(
    string FormaPagamento,
    decimal Total,
    int Quantidade);

public record FluxoCaixaDiaDto(
    DateTime Data,
    decimal SaldoInicialDia,
    decimal Entradas,
    decimal Saidas,
    decimal SaldoFinalDia);

public record FluxoCaixaResponseDto(
    decimal SaldoInicial,
    IReadOnlyList<FluxoCaixaDiaDto> Dias,
    decimal SaldoFinal,
    decimal? ProjecaoReceitaFutura);

public record CriarContaReceberRequestDto(
    string Descricao,
    decimal Valor,
    DateTime Vencimento,
    int? AgendamentoId);

public record CriarContaPagarRequestDto(
    string Fornecedor,
    string Categoria,
    string Descricao,
    decimal Valor,
    DateTime Vencimento,
    bool Recorrente);

public record BaixarContaRequestDto(
    string? FormaBaixa,
    string? Observacao);

public record FinanceiroBuscaResponseDto(
    IReadOnlyList<LancamentoCaixaResponseDto> Lancamentos,
    IReadOnlyList<ContaReceberResponseDto> ContasReceber,
    IReadOnlyList<ContaPagarResponseDto> ContasPagar);

public record FinanceiroExportacaoResponseDto(
    byte[] Conteudo,
    string ContentType,
    string NomeArquivo);

public record ConciliacaoImportacaoRequestDto(
    IReadOnlyList<ConciliacaoLinhaExtratoDto> Linhas);

public record ConciliacaoLinhaExtratoDto(
    string Descricao,
    decimal Valor,
    DateTime Data,
    string Referencia);

public record ConciliacaoItemResponseDto(
    int Id,
    int? LancamentoCaixaId,
    string DescricaoExtrato,
    decimal ValorExtrato,
    DateTime DataExtrato,
    bool Conciliado);
