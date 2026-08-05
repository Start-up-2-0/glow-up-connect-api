namespace GLOWAPI.Application.DTOs.Equipe;

public class AtualizarProfissionalEquipeRequestDto
{
    public string? NomePublico { get; set; }
    public string? Biografia { get; set; }

    /// <summary>Nova foto de apresentação. Omitir para manter a atual.</summary>
    public string? Foto { get; set; }

    public string? FotoContentType { get; set; }

    /// <summary>Quando true, remove a foto do profissional sem afetar o avatar da conta.</summary>
    public bool RemoverFoto { get; set; }
}
