using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class ContaPagar
{
    public int Id { get; set; }
    public int EstabelecimentoId { get; set; }
    public int? LancamentoCaixaId { get; set; }
    public string Fornecedor { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime Vencimento { get; set; }
    public bool Recorrente { get; set; }
    public ContaFinanceiraStatus Status { get; set; } = ContaFinanceiraStatus.Aberta;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Estabelecimento? Estabelecimento { get; set; }
    public LancamentoCaixa? LancamentoCaixa { get; set; }
}
