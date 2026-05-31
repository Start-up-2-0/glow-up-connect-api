using System.Net;

namespace GLOWAPI.Application.Mensageria;

internal static class EmailTemplateAssets
{
    private static string? _logoDataUri;
    private static string? _clockDataUri;
    private static string? _warningDataUri;

    public static string LogoDataUri => _logoDataUri ??= CarregarDataUri("email-logo-sm.png");

    public static string ClockDataUri => _clockDataUri ??= CarregarDataUri("icon-clock.png");

    public static string WarningDataUri => _warningDataUri ??= CarregarDataUri("icon-warning.png");

    private static string CarregarDataUri(string nomeArquivo)
    {
        var assembly = typeof(EmailTemplateAssets).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith(nomeArquivo, StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Asset de e-mail nao encontrado: {nomeArquivo}");

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        var base64 = Convert.ToBase64String(memoryStream.ToArray());
        return $"data:image/png;base64,{base64}";
    }
}
