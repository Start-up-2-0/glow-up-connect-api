namespace GLOWAPI.Domain.Entities;

public class EstabelecimentoComodidade
{
    public int EstabelecimentoId { get; set; }
    public int ComodidadeId { get; set; }
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;

    public Estabelecimento? Estabelecimento { get; set; }
    public Comodidade? Comodidade { get; set; }
}
