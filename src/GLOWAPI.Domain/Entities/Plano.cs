using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class Plano
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public PlanoPeriodo Periodo { get; set; }
    public int? LimiteProfissionais { get; set; }
    public int? LimiteServicos { get; set; }
    public int? LimiteAgendamentos { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Assinatura> Assinaturas { get; set; } = new List<Assinatura>();
}
