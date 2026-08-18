namespace GLOWAPI.Application.DTOs.Agendamento;

public class ProfissionalPublicoResponseDto
{
    public Guid PublicGuid { get; set; }
    public string NomePublico { get; set; } = string.Empty;

    /// <summary>Foto de apresentação do profissional (data URI). Null quando ausente.</summary>
    public string? Foto { get; set; }
}
