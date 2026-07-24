namespace GLOWAPI.Application.Helpers;

public static class TextSanitizer
{
    public static string SanitizeForWhatsApp(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Remover caracteres de controle (0x00 a 0x1F), exceto \n, \r, \t
        // e caracteres que podem ser interpretados como maliciosos ou problematicos
        var sanitizedText = new string(text.Where(c =>
            c == '\t' || c == '\n' || c == '\r' || // Permitir tab, newline, carriage return
            (c >= ' ' && c <= '~') || // Caracteres ASCII imprimíveis
            (c >= ' ' && c <= '퟿') || // Unicode imprimível
            (c >= '' && c <= '￿') // Mais Unicode
        ).ToArray());

        // Opcional: Substituir aspas para evitar problemas de parsing em algumas APIs,
        // mas a serialização JSON geralmente cuida disso.
        // sanitizedText = sanitizedText.Replace("\"", "'");

        return sanitizedText.Trim();
    }
}