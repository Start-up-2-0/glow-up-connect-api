using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class MetaProfissional
{
    public int Id { get; set; }
    public int ProfissionalEstabelecimentoId { get; set; }
    public TipoMeta TipoMeta { get; set; }
    public int? QuantidadeAtendimentos { get; set; }
    public decimal? ValorFaturamento { get; set; }
    public DateTime InicioPeriodo { get; set; }
    public DateTime FimPeriodo { get; set; }
    public MetaProfissionalStatus Status { get; set; } = MetaProfissionalStatus.Ativa;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ProfissionalEstabelecimento? ProfissionalEstabelecimento { get; set; }
}
