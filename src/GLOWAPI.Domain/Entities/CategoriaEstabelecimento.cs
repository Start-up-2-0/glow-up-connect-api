namespace GLOWAPI.Domain.Entities;

/// <summary>
/// Catálogo de categorias de estabelecimento (extensível).
/// A tabela é populada via seed (Barbearia, Salão de Beleza, etc.) e novas
/// categorias podem ser adicionadas sem alteração de código.
/// </summary>
public class CategoriaEstabelecimento
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
