namespace GLOWAPI.Application.Helpers;

public static class ConfirmacaoWhatsAppTokenHelper
{
    public static bool MensagemContemTokenConfirmacao(string textoMensagem, string telefoneRemetente)
    {
        if (string.IsNullOrWhiteSpace(textoMensagem) || string.IsNullOrWhiteSpace(telefoneRemetente))
        {
            return false;
        }

        var token = TelefoneHelper.GerarTokenConfirmacao(telefoneRemetente);
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        return textoMensagem.Contains(token, StringComparison.Ordinal);
    }

    public static bool PareceTentativaConfirmacaoPorToken(string textoMensagem)
    {
        if (string.IsNullOrWhiteSpace(textoMensagem))
        {
            return false;
        }

        return textoMensagem.Contains("NTU", StringComparison.Ordinal);
    }
}
