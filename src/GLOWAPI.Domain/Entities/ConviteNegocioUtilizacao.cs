namespace GLOWAPI.Domain.Entities;

public class ConviteNegocioUtilizacao
{
    public int Id { get; set; }
    public int ConviteNegocioId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime UtilizadoEm { get; set; } = DateTime.UtcNow;

    public ConviteNegocio? ConviteNegocio { get; set; }
    public Usuario? Usuario { get; set; }
}
