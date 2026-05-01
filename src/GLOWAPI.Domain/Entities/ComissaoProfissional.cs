using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class ComissaoProfissional
{
    public int Id { get; set; }
    public int ProfissionalEstabelecimentoId { get; set; }
    public TipoComissao TipoComissao { get; set; }
    public decimal? Percentual { get; set; }
    public decimal? ValorFixo { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime InicioVigencia { get; set; }
    public DateTime? FimVigencia { get; set; }
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ProfissionalEstabelecimento? ProfissionalEstabelecimento { get; set; }
}
