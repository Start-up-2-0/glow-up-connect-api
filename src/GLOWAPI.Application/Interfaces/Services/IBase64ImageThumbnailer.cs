namespace GLOWAPI.Application.Interfaces.Services;

/// <summary>
/// Reduz data URI / base64 de imagem para thumbnails de listagem (mapa, cards).
/// </summary>
public interface IBase64ImageThumbnailer
{
    /// <summary>
    /// Miniatura para listagens (mapa/cards). Em falha retorna string.Empty.
    /// </summary>
    string ParaListagem(string? dataUriOuBase64, int maxLadoPx = 96, int qualidadeJpeg = 72);

    /// <summary>
    /// Compacta imagem antes de persistir (logo/foto). Em falha devolve a entrada original.
    /// </summary>
    string ParaPersistencia(string dataUriOuBase64, int maxLadoPx = 512, int qualidadeJpeg = 82);
}
