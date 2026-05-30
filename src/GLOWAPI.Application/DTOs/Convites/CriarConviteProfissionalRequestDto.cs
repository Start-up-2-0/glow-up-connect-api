namespace GLOWAPI.Application.DTOs.Convites;

public class CriarConviteProfissionalRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? NomePublico { get; set; }
    public bool PodeReceberAgendamento { get; set; } = true;
}
