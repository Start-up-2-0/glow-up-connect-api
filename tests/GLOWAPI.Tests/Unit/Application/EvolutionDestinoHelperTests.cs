using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class EvolutionDestinoHelperTests
{
    [Fact]
    public void CriarCandidatosDestinoOutbound_DevePriorizarRemoteJidAlt_QuandoDisponivel()
    {
        var candidatos = EvolutionDestinoHelper.CriarCandidatosDestinoOutbound(
            "79998755111",
            "60348602310753@lid",
            "5579998755111@s.whatsapp.net");

        Assert.Equal("5579998755111@s.whatsapp.net", candidatos[0]);
        Assert.Contains("5579998755111", candidatos);
    }

    [Fact]
    public void ResolverDestinoOutbound_DeveUsarTelefone_QuandoConversaEhLid()
    {
        var destino = EvolutionDestinoHelper.ResolverDestinoOutbound(
            "79998755111",
            "60348602310753@lid");

        Assert.Equal("557998755111", destino);
    }

    [Fact]
    public void ResolverDestinoOutbound_DeveUsarTelefone_QuandoConversaNaoEhLid()
    {
        var destino = EvolutionDestinoHelper.ResolverDestinoOutbound(
            "79998755111",
            "5511988887777@s.whatsapp.net");

        Assert.Equal("557998755111", destino);
    }

    [Fact]
    public void CriarCandidatosDestinoOutbound_DevePriorizarTelefoneAntesDeLid_QuandoConversaEhLid()
    {
        var candidatos = EvolutionDestinoHelper.CriarCandidatosDestinoOutbound(
            "79991917634",
            "67268163698795@lid",
            remoteJidAlt: null);

        Assert.Equal("557991917634", candidatos[0]);
        Assert.Equal("557991917634@s.whatsapp.net", candidatos[1]);
        Assert.Equal("67268163698795@lid", candidatos[^1]);
    }

    [Fact]
    public void CriarCandidatosDestinoOutbound_DevePriorizarTelefoneSemNonoDigito_ParaEvolution()
    {
        var candidatos = EvolutionDestinoHelper.CriarCandidatosDestinoOutbound(
            "79991917634",
            remoteJidConversa: null,
            remoteJidAlt: null);

        Assert.Equal("557991917634", candidatos[0]);
        Assert.Equal("557991917634@s.whatsapp.net", candidatos[1]);
        Assert.Contains("5579991917634", candidatos);
    }

    [Fact]
    public void CriarCandidatosDestinoOutboundDeMensagem_DeveUsarPayloadLid_QuandoDestinatarioEhTelefoneCadastrado()
    {
        const string payload = """
            {
              "telefoneFallback":"5579991917634",
              "remoteJidConversa":"67268163698795@lid",
              "quotedMessageId":"ABC123",
              "quotedFromMe":true,
              "quotedTexto":"token"
            }
            """;

        var candidatos = EvolutionDestinoHelper.CriarCandidatosDestinoOutboundDeMensagem(
            "5579991917634",
            payload);

        Assert.Equal("557991917634", candidatos[0]);
        Assert.Equal("67268163698795@lid", candidatos[^1]);
    }

    [Fact]
    public void ExtrairContextoRespostaDoPayload_DeveMontarQuoted_QuandoPayloadTemDados()
    {
        const string payload = """
            {
              "telefoneFallback":"5579991917634",
              "remoteJidConversa":"67268163698795@lid",
              "quotedMessageId":"ABC123",
              "quotedFromMe":true,
              "quotedTexto":"token"
            }
            """;

        var contexto = EvolutionDestinoHelper.ExtrairContextoRespostaDoPayload(payload);

        Assert.NotNull(contexto);
        Assert.True(contexto!.TemQuoted);
        Assert.Equal("ABC123", contexto.MessageId);
        Assert.Equal("67268163698795@lid", contexto.RemoteJidConversa);
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
