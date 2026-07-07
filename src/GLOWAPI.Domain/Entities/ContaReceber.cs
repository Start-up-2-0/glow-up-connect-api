using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class ContaReceber
{
    public int Id { get; set; }
    public int EstabelecimentoId { get; set; }
    public int? AgendamentoId { get; set; }
    public int? PagamentoId { get; set; }
    public int? LancamentoCaixaId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime Vencimento { get; set; }
    public ContaFinanceiraStatus Status { get; set; } = ContaFinanceiraStatus.Aberta;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Estabelecimento? Estabelecimento { get; set; }
    public Agendamento? Agendamento { get; set; }
    public Pagamento? Pagamento { get; set; }
    public LancamentoCaixa? LancamentoCaixa { get; set; }
}
