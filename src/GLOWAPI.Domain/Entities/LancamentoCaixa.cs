using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class LancamentoCaixa
{
    public int Id { get; set; }
    public int CaixaId { get; set; }
    public int? AgendamentoId { get; set; }
    public int? PagamentoId { get; set; }
    public int? ProfissionalId { get; set; }
    public LancamentoCaixaTipo Tipo { get; set; }
    public decimal Valor { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;

    public Caixa? Caixa { get; set; }
    public Agendamento? Agendamento { get; set; }
    public Pagamento? Pagamento { get; set; }
    public Profissional? Profissional { get; set; }
}
