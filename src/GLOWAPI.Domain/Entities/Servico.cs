using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class Servico
{
    public int Id { get; set; }
    public int? EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal PrecoBase { get; set; }
    public int DuracaoMinutos { get; set; }
    public TipoServico TipoServico { get; set; } = TipoServico.Individual;
    public string? Imagem { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Estabelecimento? Estabelecimento { get; set; }
    public ICollection<ProfissionalServico> Profissionais { get; set; } = new List<ProfissionalServico>();
    public ICollection<AgendamentoItem> AgendamentoItens { get; set; } = new List<AgendamentoItem>();
}
