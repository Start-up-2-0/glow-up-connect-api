namespace GLOWAPI.Domain.Entities;

public class ConciliacaoItem
{
    public int Id { get; set; }
    public int EstabelecimentoId { get; set; }
    public int? LancamentoCaixaId { get; set; }
    public string DescricaoExtrato { get; set; } = string.Empty;
    public decimal ValorExtrato { get; set; }
    public DateTime DataExtrato { get; set; }
    public string ReferenciaExtrato { get; set; } = string.Empty;
    public bool Conciliado { get; set; }
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Estabelecimento? Estabelecimento { get; set; }
    public LancamentoCaixa? LancamentoCaixa { get; set; }
}
