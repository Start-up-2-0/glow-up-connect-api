using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Tests.Unit.Application;

public class CicloCobrancaAssinaturaServiceTests
{
    private readonly CicloCobrancaAssinaturaService _service = new(
        Microsoft.Extensions.Options.Options.Create(new AssinaturaCobrancaOptions()));

    [Fact]
    public void CalcularPrimeiroCiclo_DeveUsarAniversarioDaCriacao()
    {
        var dataReferencia = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var referencia = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var ciclo = _service.CalcularPrimeiroCiclo(dataReferencia, referencia, PlanoPeriodo.Mensal);

        Assert.Equal(new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc), ciclo.Vencimento);
        Assert.Equal(new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc), ciclo.Geracao);
        Assert.Equal(new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc), ciclo.Alerta);
    }

    [Fact]
    public void CalcularPrimeiroCiclo_DeveConsiderarReferenciaAposTrial()
    {
        var dataReferencia = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var fimTrial = new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc);
        var ciclo = _service.CalcularPrimeiroCiclo(dataReferencia, fimTrial, PlanoPeriodo.Mensal);

        Assert.Equal(new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc), ciclo.Vencimento);
    }

    [Fact]
    public void CalcularProximoCiclo_DeveAvancarUmMes()
    {
        var vencimentoAtual = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);
        var proximo = _service.CalcularProximoCiclo(vencimentoAtual, PlanoPeriodo.Mensal);

        Assert.Equal(new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc), proximo.Vencimento);
        Assert.Equal(new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc), proximo.Geracao);
    }
}
