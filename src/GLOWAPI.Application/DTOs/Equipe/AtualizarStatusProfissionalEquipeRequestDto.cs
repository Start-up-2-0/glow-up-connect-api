namespace GLOWAPI.Application.DTOs.Equipe;

public class AtualizarStatusProfissionalEquipeRequestDto
{
    public bool Ativo { get; set; }
    public bool PodeReceberAgendamento { get; set; } = true;
}
