using System.Text.RegularExpressions;

namespace GLOWAPI.Application.Mensageria;

public static partial class MensageriaLogSanitizer
{
    public static string MascararDestinatario(string destinatario, bool habilitado)
    {
        if (!habilitado || string.IsNullOrWhiteSpace(destinatario))
        {
            return destinatario;
        }

        if (destinatario.Contains('@', StringComparison.Ordinal))
        {
            var partes = destinatario.Split('@');
            if (partes.Length == 2 && partes[0].Length > 1)
            {
                return $"{partes[0][0]}***@{partes[1]}";
            }
        }

        var digitos = DigitosRegex().Replace(destinatario, "");
        if (digitos.Length >= 4)
        {
            return $"***{digitos[^4..]}";
        }

        return "***";
    }

    public static string? MascararPayload(string? payload, bool habilitado)
    {
        if (!habilitado || string.IsNullOrWhiteSpace(payload))
        {
            return payload;
        }

        return payload.Length > 200 ? $"{payload[..200]}...[truncado]" : payload;
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitosRegex();
}
