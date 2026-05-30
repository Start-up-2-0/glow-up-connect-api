namespace GLOWAPI.Application.DTOs.Equipe;

public class ConvidarProfissionalEquipeRequestDto
{
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? NomePublico { get; set; }
    public string? Biografia { get; set; }
    public string? Logo { get; set; }
    public bool PodeReceberAgendamento { get; set; } = true;
}
