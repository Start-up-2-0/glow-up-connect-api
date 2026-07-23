using System.Text;

namespace GLOWAPI.Application.Helpers;

public static class TelefoneHelper
{
    private const int TelefoneBrasilConfirmacaoDigitos = 13;

    public static string NormalizarParaWhatsApp(string telefone)
    {
        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digitos))
        {
            return string.Empty;
        }

        if (digitos.StartsWith("55", StringComparison.Ordinal) && digitos.Length >= 12)
        {
            return digitos;
        }

        if (digitos.Length is 10 or 11)
        {
            return $"55{digitos}";
        }

        return digitos;
    }

    /// <summary>
    /// Formato preferido para envio Evolution/WhatsApp: celular BR sem o nono digito apos DDI+DDD, quando aplicavel.
    /// </summary>
    public static string NormalizarParaEvolutionEnvio(string telefone)
    {
        var normalizado = NormalizarParaWhatsApp(telefone);
        if (string.IsNullOrEmpty(normalizado))
        {
            return string.Empty;
        }


        return normalizado;
    }

    /// <summary>
    /// Normaliza telefone para persistencia no banco (sempre com DDI 55 para numeros brasileiros locais).
    /// </summary>
    public static string NormalizarParaArmazenamento(string? telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone))
        {
            return string.Empty;
        }

        return NormalizarParaWhatsApp(telefone.Trim());
    }

    public static bool SaoEquivalentes(string? telefoneA, string? telefoneB)
    {
        var normalizadoA = NormalizarParaWhatsApp(telefoneA ?? string.Empty);
        var normalizadoB = NormalizarParaWhatsApp(telefoneB ?? string.Empty);

        if (string.IsNullOrEmpty(normalizadoA) || string.IsNullOrEmpty(normalizadoB))
        {
            return false;
        }

        return normalizadoA == normalizadoB;
    }

    /// <summary>
    /// Normaliza telefone para confirmacao inbound (estilo Bode): 13 digitos com nono digito apos DDI+DDD.
    /// </summary>
    public static string NormalizarParaConfirmacaoInbound(string telefone)
    {
        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digitos))
        {
            return string.Empty;
        }

        if (!digitos.StartsWith("55", StringComparison.Ordinal) && digitos.Length is 10 or 11)
        {
            digitos = $"55{digitos}";
        }

        if (digitos.Length != TelefoneBrasilConfirmacaoDigitos && digitos.Length >= 4)
        {
            var primeirosQuatro = digitos[..4];
            var resto = digitos[4..];
            digitos = $"{primeirosQuatro}9{resto}";
        }

        return digitos;
    }

    public static string GerarTokenConfirmacao(string telefone)
    {
        var normalizado = NormalizarParaConfirmacaoInbound(telefone);
        if (string.IsNullOrEmpty(normalizado))
        {
            return string.Empty;
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(normalizado));
    }

    public static string? DecodificarTokenConfirmacao(string tokenBase64)
    {
        if (string.IsNullOrWhiteSpace(tokenBase64))
        {
            return null;
        }

        try
        {
            var normalizado = Encoding.UTF8.GetString(Convert.FromBase64String(tokenBase64.Trim()));
            var digitos = new string(normalizado.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digitos))
            {
                return null;
            }

            return NormalizarParaConfirmacaoInbound(digitos);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static bool TokenCorrespondeTelefone(string tokenBase64, string telefone)
    {
        var telefoneDoToken = DecodificarTokenConfirmacao(tokenBase64);
        if (string.IsNullOrWhiteSpace(telefoneDoToken))
        {
            return false;
        }

        return SaoEquivalentes(telefoneDoToken, telefone);
    }

    public static string CriarLinkConfirmacao(string frontendBaseUrl, string tokenConfirmacao)
    {
        if (string.IsNullOrWhiteSpace(frontendBaseUrl) || string.IsNullOrWhiteSpace(tokenConfirmacao))
        {
            return string.Empty;
        }

        var baseUrl = frontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/c/{tokenConfirmacao}";
    }

    public static string CriarLinkWaMe(string numeroPlataforma, string mensagem)
    {
        var numero = NormalizarParaWhatsApp(numeroPlataforma);
        if (string.IsNullOrEmpty(numero))
        {
            return string.Empty;
        }

        var texto = Uri.EscapeDataString(mensagem.Trim());
        return $"https://wa.me/{numero}?text={texto}";
    }


}
