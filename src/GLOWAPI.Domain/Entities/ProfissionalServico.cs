namespace GLOWAPI.Domain.Entities;

public class ProfissionalServico
{
    public int Id { get; set; }
    public int ProfissionalId { get; set; }
    public int ServicoId { get; set; }
    public decimal Preco { get; set; }
    public int DuracaoMinutos { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Profissional? Profissional { get; set; }
    public Servico? Servico { get; set; }
}
