namespace GLOWAPI.Application.DTOs.Rede;

public record RedeUnidadeResumoDto(
    int EstabelecimentoId,
    string Nome,
    bool EhMatriz,
    int AgendamentosNoPeriodo,
    decimal FaturamentoPeriodo);

public record RedeResumoResponseDto(
    int AssinaturaId,
    int TotalUnidades,
    int? LimiteUnidades,
    int TotalAgendamentosNoPeriodo,
    decimal TotalFaturamentoPeriodo,
    IReadOnlyList<RedeUnidadeResumoDto> Unidades);
