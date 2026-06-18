namespace GLOWAPI.Application.DTOs.Financeiro;

public record FinanceiroResumoResponseDto(
    decimal SaldoTotal,
    decimal SaldoDisponivel,
    decimal SaldoRetido,
    decimal EntradasPeriodo,
    decimal SaidasPeriodo,
    int TotalLancamentosPeriodo,
    DateTime? PeriodoInicio,
    DateTime? PeriodoFim);
