namespace GLOWAPI.Application.Helpers;

public static class TelefoneHelper
{
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

    public static bool SaoEquivalentes(string? telefoneA, string? telefoneB)
    {
        var normalizadoA = NormalizarParaWhatsApp(telefoneA ?? string.Empty);
        var normalizadoB = NormalizarParaWhatsApp(telefoneB ?? string.Empty);

        if (string.IsNullOrEmpty(normalizadoA) || string.IsNullOrEmpty(normalizadoB))
        {
            return false;
        }

        if (normalizadoA == normalizadoB)
        {
            return true;
        }

        return SaoEquivalentesCelularBrasil(normalizadoA, normalizadoB);
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

    private static bool SaoEquivalentesCelularBrasil(string telefoneA, string telefoneB)
    {
        var varianteA = ObterVarianteCelularBrasilSemNonoDigito(telefoneA);
        var varianteB = ObterVarianteCelularBrasilSemNonoDigito(telefoneB);

        if (varianteA is null || varianteB is null)
        {
            return false;
        }

        return varianteA == varianteB
            || varianteA == telefoneB
            || varianteB == telefoneA;
    }

    private static string? ObterVarianteCelularBrasilSemNonoDigito(string telefone)
    {
        if (!telefone.StartsWith("55", StringComparison.Ordinal))
        {
            return null;
        }

        var numeroLocal = telefone[2..];

        if (numeroLocal.Length == 11 && numeroLocal[2] == '9')
        {
            return $"55{numeroLocal[..2]}{numeroLocal[3..]}";
        }

        if (numeroLocal.Length == 10)
        {
            return $"55{numeroLocal[..2]}9{numeroLocal[2..]}";
        }

        return null;
    }
}
