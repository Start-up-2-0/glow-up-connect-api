namespace GLOWAPI.Application.DTOs.Agendamento;

public class ProfissionalVitrinePublicoResponseDto
{
    public Guid PublicGuid { get; set; }
    public string NomePublico { get; set; } = string.Empty;
    public string Biografia { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
}
