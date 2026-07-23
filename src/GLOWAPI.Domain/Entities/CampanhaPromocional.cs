namespace GLOWAPI.Domain.Entities;

public class CampanhaPromocional
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int Limite { get; set; }
    public int Utilizados { get; set; }
    public int DiasTrial { get; set; }
    public decimal PercentualDescontoMensalidade { get; set; }
    public bool Ativa { get; set; } = true;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Assinatura> Assinaturas { get; set; } = new List<Assinatura>();
}
