using System.Text.Json;
using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class EvolutionDestinoHelperTests
{
    [Fact]
    public void ResolverDestinoOutbound_DeveUsarFormatoEvolution()
    {
        var destino = EvolutionDestinoHelper.ResolverDestinoOutbound("79991917634");

        Assert.Equal("557991917634", destino);
    }

    [Fact]
    public void CriarCandidatosDestinoOutbound_DeveRetornarFormatoEvolution()
    {
        var candidatos = EvolutionDestinoHelper.CriarCandidatosDestinoOutbound("79991917634");

        Assert.Single(candidatos);
        Assert.Equal("557991917634", candidatos[0]);
    }

    [Fact]
    public void CriarCandidatosDestinoOutboundDeMensagem_DeveUsarDestinatario_QuandoEhTelefone()
    {
        var candidatos = EvolutionDestinoHelper.CriarCandidatosDestinoOutboundDeMensagem(
            "5579991917634",
            payloadJson: null);

        Assert.Single(candidatos);
        Assert.Equal("557991917634", candidatos[0]);
    }

    [Fact]
    public void CriarCandidatosDestinoOutboundDeMensagem_DeveUsarTelefoneFallback_QuandoDestinatarioLegadoEhLid()
    {
        const string payload = """{"telefoneFallback":"5579998755111"}""";

        var candidatos = EvolutionDestinoHelper.CriarCandidatosDestinoOutboundDeMensagem(
            "60348602310753@lid",
            payload);

        Assert.Single(candidatos);
        Assert.Equal("557998755111", candidatos[0]);
    }

    [Fact]
    public void ExtrairTelefoneFallbackDoPayload_DeveRetornarTelefone()
    {
        const string payload = """{"telefoneFallback":"5579998755111"}""";

        var telefone = EvolutionDestinoHelper.ExtrairTelefoneFallbackDoPayload(payload);

        Assert.Equal("5579998755111", telefone);
    }
}
