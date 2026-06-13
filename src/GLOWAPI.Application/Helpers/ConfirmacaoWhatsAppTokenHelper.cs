using System.Text.RegularExpressions;

namespace GLOWAPI.Application.Helpers;

public static partial class ConfirmacaoWhatsAppTokenHelper
{
    public static bool MensagemContemTokenConfirmacao(string textoMensagem, string telefoneRemetente)
    {
        if (string.IsNullOrWhiteSpace(textoMensagem))
        {
            return false;
        }

        foreach (var token in ExtrairTokensCandidatos(textoMensagem))
        {
            if (!TokenPareceTelefoneBrasileiro(token))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(telefoneRemetente)
                && TelefoneHelper.TokenCorrespondeTelefone(token, telefoneRemetente))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(telefoneRemetente))
            {
                return true;
            }
        }

        if (string.IsNullOrWhiteSpace(telefoneRemetente))
        {
            return false;
        }

        var tokenEsperado = TelefoneHelper.GerarTokenConfirmacao(telefoneRemetente);
        return !string.IsNullOrEmpty(tokenEsperado)
            && textoMensagem.Contains(tokenEsperado, StringComparison.Ordinal);
    }

    public static bool PareceTentativaConfirmacaoPorToken(string textoMensagem)
    {
        if (string.IsNullOrWhiteSpace(textoMensagem))
        {
            return false;
        }

        return ExtrairTokensCandidatos(textoMensagem).Any(TokenPareceTelefoneBrasileiro);
    }

    public static IEnumerable<string> ExtrairTokensCandidatos(string textoMensagem)
    {
        if (string.IsNullOrWhiteSpace(textoMensagem))
        {
            yield break;
        }

        var vistos = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match match in TokenBase64Regex().Matches(textoMensagem))
        {
            var token = match.Value.Trim();
            if (vistos.Add(token))
            {
                yield return token;
            }
        }
    }

    public static bool TokenPareceTelefoneBrasileiro(string tokenBase64)
    {
        var telefone = TelefoneHelper.DecodificarTokenConfirmacao(tokenBase64);
        return !string.IsNullOrWhiteSpace(telefone)
            && telefone.StartsWith("55", StringComparison.Ordinal)
            && telefone.Length is >= 12 and <= 13;
    }

    [GeneratedRegex(@"(?<![A-Za-z0-9+/])([A-Za-z0-9+/]{8,}={0,2})(?![A-Za-z0-9+/=])", RegexOptions.Compiled)]
    private static partial Regex TokenBase64Regex();
}
