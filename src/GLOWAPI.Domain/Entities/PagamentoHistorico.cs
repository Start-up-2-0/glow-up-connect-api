using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class PagamentoHistorico
{
    public int Id { get; set; }
    public int PagamentoId { get; set; }
    public int? AssinaturaId { get; set; }
    public string Evento { get; set; } = string.Empty;
    public PagamentoStatus? StatusAnterior { get; set; }
    public PagamentoStatus StatusNovo { get; set; }
    public GatewayPagamento Gateway { get; set; }
    public string GatewayPaymentId { get; set; } = string.Empty;
    public string MetodoPagamento { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Moeda { get; set; } = "BRL";
    public string Observacao { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;

    public Pagamento? Pagamento { get; set; }
    public Assinatura? Assinatura { get; set; }
}
