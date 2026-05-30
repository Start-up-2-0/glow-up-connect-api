namespace GLOWAPI.Application.DTOs.Servicos;

public class CriarServicoRequestDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal PrecoBase { get; set; }
    public int DuracaoMinutos { get; set; }
}
