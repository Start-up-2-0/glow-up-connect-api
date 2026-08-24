namespace GLOWAPI.Domain.Entities;

public class FavoritoCliente
{
    public int Id { get; set; }
    public int UsuarioClienteId { get; set; }
    public int EstabelecimentoId { get; set; }
    public int? ProfissionalId { get; set; }
    public int ProfissionalChave { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Usuario? UsuarioCliente { get; set; }
    public Estabelecimento? Estabelecimento { get; set; }
    public Profissional? Profissional { get; set; }
}
