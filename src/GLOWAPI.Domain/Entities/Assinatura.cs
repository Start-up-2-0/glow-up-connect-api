using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class Assinatura
{
    public int Id { get; set; }
    public int PlanoId { get; set; }
    public int? PlanoAlteracaoPendenteId { get; set; }
    public int? EstabelecimentoId { get; set; }
    public int? CampanhaPromocionalId { get; set; }
    public int DiaVencimento { get; set; }
    public DateTime DataReferenciaCiclo { get; set; }
    public DateTime? ProximaDataVencimento { get; set; }
    public DateTime? ProximaDataGeracaoCobranca { get; set; }
    public DateTime? ProximaDataAlerta { get; set; }
    public DateTime? UltimoAlertaFaturaEm { get; set; }
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

    /// <summary>
    /// Dados de onboarding aguardando pagamento (Checkout Pro). Materializado no webhook aprovado.
    /// </summary>
    public string? OnboardingPendenteJson { get; set; }

    public Plano? Plano { get; set; }
    public Plano? PlanoAlteracaoPendente { get; set; }
    public CampanhaPromocional? CampanhaPromocional { get; set; }
    public Estabelecimento? Estabelecimento { get; set; }
    public ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
    public ICollection<AssinaturaHistorico> Historicos { get; set; } = new List<AssinaturaHistorico>();
    public ICollection<AssinaturaRecorrenciaHistorico> RecorrenciasHistorico { get; set; } = new List<AssinaturaRecorrenciaHistorico>();
}
