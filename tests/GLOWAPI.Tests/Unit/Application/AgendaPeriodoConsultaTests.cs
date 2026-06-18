using GLOWAPI.Application.Helpers;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Tests.Unit.Application;

public class AgendaPeriodoConsultaTests
{
    private static readonly DateTime Referencia = new(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ValidarIntervaloPersonalizado_DeveRejeitarIntervaloMaiorQueUmAno()
    {
        var inicio = new DateTime(2024, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2025, 2, 11, 23, 59, 59, DateTimeKind.Utc);

        var ex = Assert.Throws<AgendaPeriodoConsultaInvalidoException>(() =>
            AgendaPeriodoConsulta.ValidarIntervaloPersonalizado(inicio, fim, Referencia));

        Assert.Equal("O intervalo nao pode ultrapassar 1 ano.", ex.Message);
    }

    [Fact]
    public void ValidarIntervaloPersonalizado_DeveAceitarIntervaloDeUmAnoMenosUmDia()
    {
        var inicio = new DateTime(2024, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2025, 2, 10, 23, 59, 59, DateTimeKind.Utc);

        var exception = Record.Exception(() =>
            AgendaPeriodoConsulta.ValidarIntervaloPersonalizado(inicio, fim, Referencia));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidarIntervaloPersonalizado_DeveRejeitarDataFinalAnteriorAInicial()
    {
        var inicio = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2025, 3, 9, 0, 0, 0, DateTimeKind.Utc);

        Assert.Throws<AgendaPeriodoConsultaInvalidoException>(() =>
            AgendaPeriodoConsulta.ValidarIntervaloPersonalizado(inicio, fim, Referencia));
    }

    [Fact]
    public void ValidarIntervaloPersonalizado_DeveRejeitarDataFinalNoFuturo()
    {
        var inicio = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

        Assert.Throws<AgendaPeriodoConsultaInvalidoException>(() =>
            AgendaPeriodoConsulta.ValidarIntervaloPersonalizado(inicio, fim, Referencia));
    }

    [Fact]
    public void ResolverIntervaloMesAtualUtc_DeveValidarQuandoIntervaloPersonalizado()
    {
        var inicio = new DateTime(2024, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2025, 2, 11, 23, 59, 59, DateTimeKind.Utc);

        Assert.Throws<AgendaPeriodoConsultaInvalidoException>(() =>
            AgendaPeriodoConsulta.ResolverIntervaloMesAtualUtc(inicio, fim, intervaloPersonalizado: true));
    }

    [Fact]
    public void ResolverIntervaloMesAtualUtc_DeveAceitarMesCompletoSemIntervaloPersonalizado()
    {
        var inicio = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc);

        var (inicioResolvido, fimResolvido) = AgendaPeriodoConsulta.ResolverIntervaloMesAtualUtc(inicio, fim);

        Assert.Equal(inicio, inicioResolvido);
        Assert.Equal(fim, fimResolvido);
    }
}
