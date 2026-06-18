using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class Pagamento
{
    public int Id { get; set; }
    public int? AgendamentoId { get; set; }
    public int? AssinaturaId { get; set; }
    public GatewayPagamento Gateway { get; set; }
    public string GatewayPaymentId { get; set; } = string.Empty;
    public string ReferenciaInterna { get; set; } = string.Empty;
    public string MetodoPagamento { get; set; } = string.Empty;
    public PagamentoStatus Status { get; set; } = PagamentoStatus.Pendente;
    public TipoCobrancaAssinatura? TipoCobranca { get; set; }
    public int NumeroCiclo { get; set; } = 1;
    public DateTime? DataVencimento { get; set; }
    public DateTime? DataGeracao { get; set; }
    public DateTime? CicloInicio { get; set; }
    public DateTime? CicloFim { get; set; }
    public decimal Valor { get; set; }
    public string Moeda { get; set; } = "BRL";
    public DateTime? PagoEm { get; set; }
    public DateTime? ExpiraEm { get; set; }
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Agendamento? Agendamento { get; set; }
    public Assinatura? Assinatura { get; set; }
    public ICollection<LancamentoCaixa> LancamentosCaixa { get; set; } = new List<LancamentoCaixa>();
    public ICollection<PagamentoHistorico> Historicos { get; set; } = new List<PagamentoHistorico>();
    public ICollection<AssinaturaHistorico> AssinaturasHistorico { get; set; } = new List<AssinaturaHistorico>();
    public ICollection<AssinaturaRecorrenciaHistorico> RecorrenciasHistorico { get; set; } = new List<AssinaturaRecorrenciaHistorico>();
}
