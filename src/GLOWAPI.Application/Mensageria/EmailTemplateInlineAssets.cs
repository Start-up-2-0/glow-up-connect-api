using GLOWAPI.Application.Models.Mensageria;

namespace GLOWAPI.Application.Mensageria;

public static class EmailTemplateInlineAssets
{
    public const string LogoContentId = "glow-email-logo";
    public const string ClockContentId = "glow-email-clock";
    public const string WarningContentId = "glow-email-warning";

    private static readonly IReadOnlyDictionary<string, AssetDefinicao> Catalogo =
        new Dictionary<string, AssetDefinicao>(StringComparer.Ordinal)
        {
            [LogoContentId] = new("email-logo-sm.png", "image/png"),
            [ClockContentId] = new("icon-clock.png", "image/png"),
            [WarningContentId] = new("icon-warning.png", "image/png")
        };

    private static readonly Dictionary<string, byte[]> CacheBytes = new(StringComparer.Ordinal);

    public static string Src(string contentId) => $"cid:{contentId}";

    public static string LogoSrc => Src(LogoContentId);

    public static string ClockSrc => Src(ClockContentId);

    public static string WarningSrc => Src(WarningContentId);

    public static IReadOnlyList<EmailAnexoInline> ResolverReferenciados(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return Array.Empty<EmailAnexoInline>();
        }

        var anexos = new List<EmailAnexoInline>();

        foreach (var (contentId, definicao) in Catalogo)
        {
            if (!html.Contains($"cid:{contentId}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            anexos.Add(new EmailAnexoInline(
                contentId,
                definicao.Filename,
                definicao.ContentType,
                CarregarBytes(definicao.Filename)));
        }

        return anexos;
    }

    private static byte[] CarregarBytes(string nomeArquivo)
    {
        lock (CacheBytes)
        {
            if (CacheBytes.TryGetValue(nomeArquivo, out var cached))
            {
                return cached;
            }

            var assembly = typeof(EmailTemplateInlineAssets).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .First(n => n.EndsWith(nomeArquivo, StringComparison.OrdinalIgnoreCase));

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Asset de e-mail nao encontrado: {nomeArquivo}");

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var bytes = memoryStream.ToArray();
            CacheBytes[nomeArquivo] = bytes;
            return bytes;
        }
    }

    private sealed record AssetDefinicao(string Filename, string ContentType);
}
