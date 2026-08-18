using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

/// <summary>
/// Catálogo de categorias do marketplace, separado por tipo de operação
/// (loja/estabelecimento vs profissional autônomo).
/// </summary>
public class CategoriaEstabelecimento
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoAssinatura TipoAssinatura { get; set; } = TipoAssinatura.Estabelecimento;
    public bool Ativo { get; set; } = true;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
