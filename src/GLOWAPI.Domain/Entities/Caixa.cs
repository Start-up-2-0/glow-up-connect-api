namespace GLOWAPI.Domain.Entities;

public class Caixa
{
    public int Id { get; set; }
    public int? EstabelecimentoId { get; set; }
    public decimal SaldoTotal { get; set; }
    public decimal SaldoDisponivel { get; set; }
    public decimal SaldoRetido { get; set; }
    public bool ExigirSessaoCaixaAberta { get; set; }
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Estabelecimento? Estabelecimento { get; set; }
    public ICollection<LancamentoCaixa> Lancamentos { get; set; } = new List<LancamentoCaixa>();
    public ICollection<SessaoCaixa> Sessoes { get; set; } = new List<SessaoCaixa>();
}
