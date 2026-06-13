using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class EvolutionDestinoHelperTests
{
    [Fact]
    public void ResolverDestinoOutbound_DeveUsarTelefone_QuandoConversaEhLid()
    {
        var destino = EvolutionDestinoHelper.ResolverDestinoOutbound(
            "79998755111",
            "60348602310753@lid");

        Assert.Equal("5579998755111", destino);
    }

    [Fact]
    public void ResolverDestinoOutbound_DeveUsarTelefone_QuandoConversaNaoEhLid()
    {
        var destino = EvolutionDestinoHelper.ResolverDestinoOutbound(
            "79998755111",
            "5511988887777@s.whatsapp.net");

        Assert.Equal("5579998755111", destino);
    }

    [Fact]
    public void CriarPayloadOutbound_DeveIncluirTelefoneFallback_QuandoConversaEhLid()
    {
        var payload = EvolutionDestinoHelper.CriarPayloadOutbound(
            "79998755111",
            "60348602310753@lid");

        Assert.NotNull(payload);
        Assert.Contains("5579998755111", payload);
        Assert.Contains("60348602310753@lid", payload);
    }

    [Fact]
    public void ExtrairTelefoneFallbackDoPayload_DeveRetornarTelefone()
    {
        const string payload = """{"telefoneFallback":"5579998755111","remoteJidConversa":"60348602310753@lid"}""";

        var telefone = EvolutionDestinoHelper.ExtrairTelefoneFallbackDoPayload(payload);

        Assert.Equal("5579998755111", telefone);
    }
}
