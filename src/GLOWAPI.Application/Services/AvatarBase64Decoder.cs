using System.Text.RegularExpressions;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Exceptions.Usuario;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public partial class AvatarBase64Decoder : IAvatarBase64Decoder
{
    private readonly AvatarOptions _options;
    private readonly IBase64ImageThumbnailer _thumbnailer;

    public AvatarBase64Decoder(IOptions<AvatarOptions> options)
        : this(options, new Base64ImageThumbnailer())
    {
    }

    public AvatarBase64Decoder(IOptions<AvatarOptions> options, IBase64ImageThumbnailer thumbnailer)
    {
        _options = options.Value;
        _thumbnailer = thumbnailer;
    }

    public string ValidarENormalizar(string avatarBase64, string? avatarContentType)
    {
        if (string.IsNullOrWhiteSpace(avatarBase64))
        {
            throw new AvatarInvalidoException("Avatar base64 e obrigatorio quando informado.");
        }

        var contentType = avatarContentType ?? string.Empty;
        var payload = avatarBase64.Trim().Replace(" ", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);

        var dataUriMatch = DataUriRegex().Match(payload);
        if (dataUriMatch.Success)
        {
            contentType = dataUriMatch.Groups["type"].Value;
            payload = dataUriMatch.Groups["data"].Value;
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new AvatarInvalidoException("Informe avatarContentType ou use data URI (data:image/...;base64,...).");
        }

        if (!_options.TiposPermitidos.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new AvatarInvalidoException("Tipo de imagem nao permitido.");
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            throw new AvatarInvalidoException("Base64 do avatar invalido.");
        }

        if (bytes.Length == 0 || bytes.Length > _options.MaxSizeBytes)
        {
            throw new AvatarInvalidoException($"Avatar deve ter entre 1 e {_options.MaxSizeBytes} bytes decodificados.");
        }

        if (!ValidarAssinatura(bytes, contentType))
        {
            throw new AvatarInvalidoException("Conteudo do avatar nao corresponde ao tipo informado.");
        }

        return _thumbnailer.ParaPersistencia($"data:{contentType};base64,{payload}");
    }

    private static bool ValidarAssinatura(byte[] bytes, string contentType)
    {
        if (bytes.Length < 4)
        {
            return false;
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => bytes[0] == 0xFF && bytes[1] == 0xD8,
            "image/png" => bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47,
            "image/webp" => bytes.Length >= 12
                && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
                && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50,
            _ => false
        };
    }

    [GeneratedRegex(@"^data:(?<type>image\/[a-zA-Z0-9.+-]+);base64,(?<data>[A-Za-z0-9+/=]+)$", RegexOptions.IgnoreCase)]
    private static partial Regex DataUriRegex();
}
