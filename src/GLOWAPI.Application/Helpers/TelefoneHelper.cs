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
        return !string.IsNullOrEmpty(normalizadoA)
            && normalizadoA == normalizadoB;
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
