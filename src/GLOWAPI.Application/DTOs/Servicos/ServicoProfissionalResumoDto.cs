namespace GLOWAPI.Application.DTOs.Servicos;

public class ServicoProfissionalResumoDto
{
    public int ProfissionalId { get; set; }
    public decimal Preco { get; set; }
    public int DuracaoMinutos { get; set; }
    public bool Ativo { get; set; }
}
