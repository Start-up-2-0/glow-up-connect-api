using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class Profissional
{
    public int Id { get; set; }
    public Guid PublicGuid { get; set; } = Guid.NewGuid();
    public int UsuarioId { get; set; }
    public string NomePublico { get; set; } = string.Empty;
    public string Biografia { get; set; } = string.Empty;
    public ProfessionalType TipoProfissional { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Usuario? Usuario { get; set; }
    public ICollection<ProfissionalEstabelecimento> Estabelecimentos { get; set; } = new List<ProfissionalEstabelecimento>();
    public Endereco? Endereco { get; set; }
}
