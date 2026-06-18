namespace GLOWAPI.Application.DTOs.Equipe;

public class CadastrarProfissionalVitrineRequestDto
{
    public string NomePublico { get; set; } = string.Empty;
    public string? Biografia { get; set; }
    public string? Logo { get; set; }
}
