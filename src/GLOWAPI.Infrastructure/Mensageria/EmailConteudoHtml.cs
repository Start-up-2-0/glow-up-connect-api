using System.Net;

namespace GLOWAPI.Infrastructure.Mensageria;

internal static class EmailConteudoHtml
{
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
}
