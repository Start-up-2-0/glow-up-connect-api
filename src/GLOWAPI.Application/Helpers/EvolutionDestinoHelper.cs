namespace GLOWAPI.Application.Helpers;

public static class EvolutionDestinoHelper
{
    /// <summary>
    /// Evolution v1.7.x precisa responder no mesmo JID da conversa inbound.
    /// Contatos @lid geram bolhas vazias ou falham quando o envio usa apenas o telefone cadastrado.
    /// </summary>
    public static string ResolverDestinoOutbound(string telefoneCadastrado, string? remoteJidConversa)
    {
        if (EvolutionWebhookParser.EhRemoteJidLid(remoteJidConversa))
        {
            return remoteJidConversa!.Trim();
        }

        return TelefoneHelper.NormalizarParaWhatsApp(telefoneCadastrado);
    }
}
