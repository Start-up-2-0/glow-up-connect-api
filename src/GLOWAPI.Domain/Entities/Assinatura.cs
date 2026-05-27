using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class Assinatura
{
    public int Id { get; set; }
    public int PlanoId { get; set; }
    public int? PlanoAlteracaoPendenteId { get; set; }
    public int? EstabelecimentoId { get; set; }
    public int? ProfissionalAutonomoId { get; set; }
    public AssinaturaStatus Status { get; set; } = AssinaturaStatus.PendentePagamento;
    public DateTime Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public bool RenovacaoAutomatica { get; set; } = true;
    public GatewayPagamento Gateway { get; set; }
    public string GatewaySubscriptionId { get; set; } = string.Empty;
    public string GatewayCustomerId { get; set; } = string.Empty;
    public int? UltimoPagamentoId { get; set; }
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CanceladoEm { get; set; }

    public Plano? Plano { get; set; }
    public Plano? PlanoAlteracaoPendente { get; set; }
    public Estabelecimento? Estabelecimento { get; set; }
    public Profissional? ProfissionalAutonomo { get; set; }
    public ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
}
