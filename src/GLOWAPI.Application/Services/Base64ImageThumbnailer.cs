using System.Text.RegularExpressions;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace GLOWAPI.Application.Services;

public partial class Base64ImageThumbnailer : IBase64ImageThumbnailer
{
    private readonly AvatarOptions _options;

    public Base64ImageThumbnailer(IOptions<AvatarOptions> options)
    {
        _options = options.Value;
    }

    public string ParaListagem(string? dataUriOuBase64, int maxLadoPx = 96, int qualidadeJpeg = 72)
    {
        if (string.IsNullOrWhiteSpace(dataUriOuBase64))
        {
            return string.Empty;
        }

        return Redimensionar(dataUriOuBase64, maxLadoPx, qualidadeJpeg, falhaRetornaVazio: true);
    }

    public string ParaPersistencia(string dataUriOuBase64, int? maxLadoPx = null, int? qualidadeJpeg = null)
    {
        if (string.IsNullOrWhiteSpace(dataUriOuBase64))
        {
            return dataUriOuBase64;
        }

        var lado = maxLadoPx ?? _options.PersistenciaMaxLadoPx;
        var qualidade = qualidadeJpeg ?? _options.PersistenciaQualidadeJpeg;
        var resultado = Redimensionar(dataUriOuBase64, lado, qualidade, falhaRetornaVazio: false);
        return string.IsNullOrEmpty(resultado) ? dataUriOuBase64 : resultado;
    }

    private static string Redimensionar(
        string dataUriOuBase64,
        int maxLadoPx,
        int qualidadeJpeg,
        bool falhaRetornaVazio)
    {
        if (!TentarExtrairBytes(dataUriOuBase64, out var bytes) || bytes.Length == 0)
        {
            return falhaRetornaVazio ? string.Empty : dataUriOuBase64;
        }

        // Já é pequeno o bastante — evita reprocessar em listagens.
        if (falhaRetornaVazio && bytes.Length <= 12_000 && maxLadoPx <= 128)
        {
            return AssegurarDataUri(dataUriOuBase64, bytes);
        }

        try
        {
            using var image = Image.Load(bytes);
            var maiorLado = Math.Max(image.Width, image.Height);

            // Já cabe no alvo e é leve: mantém o formato original (evita reencode desnecessário).
            if (maiorLado <= maxLadoPx && bytes.Length <= (falhaRetornaVazio ? 12_000 : 40_000))
            {
                return AssegurarDataUri(dataUriOuBase64, bytes);
            }

            if (maiorLado > maxLadoPx)
            {
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxLadoPx, maxLadoPx),
                }));
            }

            using var ms = new MemoryStream();
            image.Save(ms, new JpegEncoder { Quality = Math.Clamp(qualidadeJpeg, 40, 90) });
            var payload = Convert.ToBase64String(ms.ToArray());
            return $"data:image/jpeg;base64,{payload}";
        }
        catch
        {
            return falhaRetornaVazio ? string.Empty : dataUriOuBase64;
        }
    }

    private static string AssegurarDataUri(string original, byte[] bytes)
    {
        if (original.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return original.Trim();
        }

        var mime = DetectarMime(bytes);
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }

    private static bool TentarExtrairBytes(string input, out byte[] bytes)
    {
        bytes = [];
        var payload = input.Trim().Replace(" ", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
        var match = DataUriRegex().Match(payload);
        if (match.Success)
        {
            payload = match.Groups["data"].Value;
        }

        try
        {
            bytes = Convert.FromBase64String(payload);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string DetectarMime(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8)
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            return "image/png";
        }

        if (bytes.Length >= 12
            && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
            && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
        {
            return "image/webp";
        }

        return "image/jpeg";
    }

    [GeneratedRegex(@"^data:(?<type>image\/[a-zA-Z0-9.+-]+);base64,(?<data>[A-Za-z0-9+/=]+)$", RegexOptions.IgnoreCase)]
    private static partial Regex DataUriRegex();
}
