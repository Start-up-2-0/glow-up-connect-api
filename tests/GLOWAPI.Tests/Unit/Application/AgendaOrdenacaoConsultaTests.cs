using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class AgendaOrdenacaoConsultaTests
{
    [Theory]
    [InlineData("proximos", AgendaOrdenacaoConsulta.AtendimentoAsc)]
    [InlineData("recentes", AgendaOrdenacaoConsulta.CriacaoDesc)]
    [InlineData("atendimento_desc", AgendaOrdenacaoConsulta.AtendimentoDesc)]
    [InlineData(null, AgendaOrdenacaoConsulta.AtendimentoDesc)]
    public void Normalizar_DeveMapearValoresConhecidos(string? entrada, string esperado)
    {
        Assert.Equal(esperado, AgendaOrdenacaoConsulta.Normalizar(entrada));
    }
}
