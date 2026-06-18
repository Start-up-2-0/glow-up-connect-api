namespace GLOWAPI.Application.DTOs.Equipe;

public class AtualizarProfissionalVitrineRequestDto
{
    public string NomePublico { get; set; } = string.Empty;
    public string? Biografia { get; set; }
    public string? Logo { get; set; }
}
