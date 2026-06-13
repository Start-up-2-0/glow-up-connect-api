namespace GLOWAPI.Domain.Entities;

public class AssinaturaEstabelecimento
{
    public int Id { get; set; }
    public int AssinaturaId { get; set; }
    public int EstabelecimentoId { get; set; }
    public bool EhMatriz { get; set; }
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;

    public Assinatura? Assinatura { get; set; }
    public Estabelecimento? Estabelecimento { get; set; }
}
