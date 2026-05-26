using System.Net;

namespace GLOWAPI.Infrastructure.Mensageria;

internal static class EmailConteudoHtml
{
    public static string ConteudoParaHtml(string conteudo)
    {
        if (string.IsNullOrEmpty(conteudo))
        {
            return string.Empty;
        }

        return PareceHtml(conteudo)
            ? conteudo
            : TextoParaHtml(conteudo);
    }

    public static string TextoParaHtml(string texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        var escaped = WebUtility.HtmlEncode(texto);
        return escaped.Replace("\r\n", "<br/>", StringComparison.Ordinal)
            .Replace("\n", "<br/>", StringComparison.Ordinal)
            .Replace("\r", "<br/>", StringComparison.Ordinal);
    }

    private static bool PareceHtml(string conteudo)
    {
        var trimmed = conteudo.TrimStart();
        return trimmed.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
    }
}
