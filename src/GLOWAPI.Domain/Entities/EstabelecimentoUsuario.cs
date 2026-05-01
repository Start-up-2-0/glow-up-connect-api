using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class EstabelecimentoUsuario
{
    public int Id { get; set; }
    public int EstabelecimentoId { get; set; }
    public int UsuarioId { get; set; }
    public EstablishmentUserRole RoleNoEstabelecimento { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Estabelecimento? Estabelecimento { get; set; }
    public Usuario? Usuario { get; set; }
}
