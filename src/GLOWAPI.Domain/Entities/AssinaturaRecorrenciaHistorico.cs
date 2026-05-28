namespace GLOWAPI.Domain.Entities;

public class AssinaturaRecorrenciaHistorico
{
    public int Id { get; set; }
    public int AssinaturaId { get; set; }
    public int? PagamentoId { get; set; }
    public string Evento { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CicloInicio { get; set; }
    public DateTime? CicloFim { get; set; }
    public bool RenovacaoAutomatica { get; set; }
    public string Observacao { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;

    public Assinatura? Assinatura { get; set; }
    public Pagamento? Pagamento { get; set; }
}
