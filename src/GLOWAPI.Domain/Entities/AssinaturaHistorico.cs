using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class AssinaturaHistorico
{
    public int Id { get; set; }
    public int AssinaturaId { get; set; }
    public int? PagamentoId { get; set; }
    public string Evento { get; set; } = string.Empty;
    public AssinaturaStatus? StatusAnterior { get; set; }
    public AssinaturaStatus StatusNovo { get; set; }
    public int? PlanoId { get; set; }
    public int? PlanoAlteracaoPendenteId { get; set; }
    public string Observacao { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;

    public Assinatura? Assinatura { get; set; }
    public Pagamento? Pagamento { get; set; }
}
