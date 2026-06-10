using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class AgendaDateTimeHelperTests
{
    [Fact]
    public void ResolverInicio_DevePriorizarInicioSelecionado_QuandoHorarioInicioEstiverDeslocado()
    {
        var data = new DateOnly(2026, 6, 15);
        var horarioErrado = new TimeOnly(16, 0);
        var inicioSelecionado = new DateTime(2026, 6, 15, 18, 0, 0, DateTimeKind.Utc);

        var inicio = AgendaDateTimeHelper.ResolverInicio(data, horarioErrado, inicioSelecionado);

        Assert.Equal(new DateTime(2026, 6, 15, 18, 0, 0, DateTimeKind.Utc), inicio);
    }

    [Fact]
    public void ResolverInicio_DeveComporDataHorario_QuandoInicioSelecionadoNaoInformado()
    {
        var data = new DateOnly(2026, 6, 15);
        var horario = new TimeOnly(18, 0);

        var inicio = AgendaDateTimeHelper.ResolverInicio(data, horario, inicioSelecionado: null);

        Assert.Equal(new DateTime(2026, 6, 15, 18, 0, 0, DateTimeKind.Utc), inicio);
    }
}
