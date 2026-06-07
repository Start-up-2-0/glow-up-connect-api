using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Exceptions.Assinatura;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Tests.Unit.Application;

public class CicloCobrancaAssinaturaServiceTests
{
    private readonly CicloCobrancaAssinaturaService _service = new(
        Options.Create(new AssinaturaCobrancaOptions()));

    [Fact]
    public void CalcularPrimeiroCiclo_DeveUsarDia15AposTrial()
    {
        var referencia = new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);
        var ciclo = _service.CalcularPrimeiroCiclo(15, referencia);

        Assert.Equal(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc), ciclo.Vencimento);
        Assert.Equal(new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc), ciclo.Geracao);
        Assert.Equal(new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc), ciclo.Alerta);
    }

    [Fact]
    public void CalcularPrimeiroCiclo_DeveAvancarMes_QuandoDiaJaPassou()
    {
        var referencia = new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc);
        var ciclo = _service.CalcularPrimeiroCiclo(5, referencia);

        Assert.Equal(new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc), ciclo.Vencimento);
    }

    [Fact]
    public void ValidarDiaVencimento_DeveLancarExcecao_ParaDiaInvalido()
    {
        Assert.Throws<DiaVencimentoAssinaturaInvalidoException>(() => _service.ValidarDiaVencimento(7));
    }

    [Fact]
    public void CalcularProximoCiclo_DeveAvancarParaProximoMes()
    {
        var vencimentoAtual = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);
        var proximo = _service.CalcularProximoCiclo(15, vencimentoAtual);

        Assert.Equal(new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc), proximo.Vencimento);
    }
}
