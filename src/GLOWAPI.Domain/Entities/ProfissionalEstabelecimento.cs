namespace GLOWAPI.Domain.Entities;

public class ProfissionalEstabelecimento
{
    public int Id { get; set; }
    public int ProfissionalId { get; set; }
    public int EstabelecimentoId { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataEntrada { get; set; } = DateTime.UtcNow;
    public DateTime? DataSaida { get; set; }
    public bool PodeReceberAgendamento { get; set; } = true;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Profissional? Profissional { get; set; }
    public Estabelecimento? Estabelecimento { get; set; }
    public ICollection<ComissaoProfissional> Comissoes { get; set; } = new List<ComissaoProfissional>();
    public ICollection<MetaProfissional> Metas { get; set; } = new List<MetaProfissional>();
}
