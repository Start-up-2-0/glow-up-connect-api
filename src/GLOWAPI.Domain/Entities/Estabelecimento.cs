namespace GLOWAPI.Domain.Entities;

public class Estabelecimento
{
    public int Id { get; set; }
    public Guid PublicGuid { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<EstabelecimentoUsuario> Usuarios { get; set; } = new List<EstabelecimentoUsuario>();
    public ICollection<ProfissionalEstabelecimento> Profissionais { get; set; } = new List<ProfissionalEstabelecimento>();
    public ICollection<Servico> Servicos { get; set; } = new List<Servico>();
    public Endereco? Endereco { get; set; }
}
