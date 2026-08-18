using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class Meta
{
    public int Id { get; set; }
    public int EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoMeta TipoMeta { get; set; }
    public decimal ValorMeta { get; set; }
    public decimal PercentualComissao { get; set; }
    public bool Ativa { get; set; } = true;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Estabelecimento? Estabelecimento { get; set; }
}
