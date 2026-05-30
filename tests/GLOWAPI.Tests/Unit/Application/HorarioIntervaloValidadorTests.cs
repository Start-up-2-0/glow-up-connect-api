using GLOWAPI.Application.Validators;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Tests.Unit.Application;

public class HorarioIntervaloValidadorTests
{
    [Fact]
    public void IntervalosConflitam_DeveDetectarSobreposicao()
    {
        var conflita = HorarioIntervaloValidador.IntervalosConflitam(
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            new TimeOnly(11, 0),
            new TimeOnly(13, 0));

        Assert.True(conflita);
    }

    [Fact]
    public void IntervalosConflitam_DevePermitirIntervalosSeparadosNoMesmoDia()
    {
        var conflita = HorarioIntervaloValidador.IntervalosConflitam(
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            new TimeOnly(17, 0));

        Assert.False(conflita);
    }

    [Fact]
    public void ValidarIntervalo_DeveLancarExcecao_QuandoHoraFimNaoEhMaiorQueInicio()
    {
        Assert.Throws<HorarioAtendimentoInvalidoException>(() =>
            HorarioIntervaloValidador.ValidarIntervalo(new TimeOnly(12, 0), new TimeOnly(9, 0)));
    }
}
