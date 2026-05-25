namespace GLOWAPI.Application.Interfaces.Services;

public interface IAvatarBase64Decoder
{
    /// <summary>
    /// Valida o base64 e retorna data URI normalizado para persistencia no banco.
    /// </summary>
    string ValidarENormalizar(string avatarBase64, string? avatarContentType);
}
