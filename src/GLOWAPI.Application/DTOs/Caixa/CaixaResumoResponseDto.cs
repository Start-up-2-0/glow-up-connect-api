using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Caixa;

public record CaixaResumoResponseDto(
    int Id,
    int EstabelecimentoId,
    decimal SaldoTotal,
    decimal SaldoDisponivel,
    decimal SaldoRetido)
{
    public static CaixaResumoResponseDto From(GLOWAPI.Domain.Entities.Caixa caixa) =>
        new(
            caixa.Id,
            caixa.EstabelecimentoId ?? 0,
            caixa.SaldoTotal,
            caixa.SaldoDisponivel,
            caixa.SaldoRetido);
}
