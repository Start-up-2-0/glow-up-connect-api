using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class EvolutionDestinoHelperTests
{
    [Fact]
    public void ResolverDestinoOutbound_DeveUsarRemoteJidLid_QuandoConversaEhLid()
    {
        const string remoteJid = "60348602310753@lid";

        var destino = EvolutionDestinoHelper.ResolverDestinoOutbound("5579998755111", remoteJid);

        Assert.Equal(remoteJid, destino);
    }

    [Fact]
    public void ResolverDestinoOutbound_DeveUsarTelefone_QuandoConversaNaoEhLid()
    {
        var destino = EvolutionDestinoHelper.ResolverDestinoOutbound(
            "79998755111",
            "5511988887777@s.whatsapp.net");

        Assert.Equal("5579998755111", destino);
    }
}
