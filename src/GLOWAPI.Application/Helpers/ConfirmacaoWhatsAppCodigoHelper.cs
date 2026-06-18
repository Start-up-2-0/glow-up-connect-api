using GLOWAPI.Application.Interfaces.Services;

namespace GLOWAPI.Application.Helpers;

public static class ConfirmacaoWhatsAppCodigoHelper
{
    public static bool MensagemContemCodigoValido(
        string textoMensagem,
        string? codigoHash,
        int digitosCodigo,
        IGlowTokenService tokenService)
    {
        if (string.IsNullOrWhiteSpace(codigoHash) || string.IsNullOrWhiteSpace(textoMensagem))
        {
            return false;
        }

        foreach (var candidato in ExtrairCandidatosCodigo(textoMensagem, digitosCodigo))
        {
            if (tokenService.HashToken(candidato) == codigoHash)
            {
                return true;
            }
        }

        return false;
    }

    public static bool PareceTentativaConfirmacao(string textoMensagem) =>
        PareceTentativaConfirmacaoPorCodigo(textoMensagem)
        || ConfirmacaoWhatsAppTokenHelper.PareceTentativaConfirmacaoPorToken(textoMensagem);

    public static bool PareceTentativaConfirmacaoPorCodigo(string textoMensagem) =>
        !string.IsNullOrWhiteSpace(textoMensagem)
        && textoMensagem.Contains("GLOW", StringComparison.OrdinalIgnoreCase);

    public static IEnumerable<string> ExtrairCandidatosCodigo(string texto, int digitosCodigo)
    {
        var numeros = new List<string>();
        var buffer = new List<char>();

        foreach (var caractere in texto)
        {
            if (char.IsDigit(caractere))
            {
                buffer.Add(caractere);
                continue;
            }

            if (buffer.Count > 0)
            {
                numeros.Add(new string(buffer.ToArray()));
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
        {
            numeros.Add(new string(buffer.ToArray()));
        }

        foreach (var numero in numeros)
        {
            if (numero.Length == digitosCodigo)
            {
                yield return numero;
            }

            if (numero.Length > digitosCodigo)
            {
                yield return numero[^digitosCodigo..];
            }
        }
    }
}
