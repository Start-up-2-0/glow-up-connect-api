using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class Profissional
{
    public int Id { get; set; }
    public Guid PublicGuid { get; set; } = Guid.NewGuid();
    public int? UsuarioId { get; set; }
    public string NomePublico { get; set; } = string.Empty;
    public string Biografia { get; set; } = string.Empty;

    /// <summary>
    /// Foto de apresentação do profissional perante clientes e agenda.
    /// Independente de <see cref="Usuario.AvatarBase64"/> (avatar da conta de acesso).
    /// Armazenada como data URI base64; vazia quando não definida.
    /// </summary>
    public string Logo { get; set; } = string.Empty;

    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ProfessionalType TipoProfissional { get; set; }
    public bool Ativo { get; set; } = true;
    public decimal? NotaMedia { get; set; }
    public int TotalAvaliacoes { get; set; }
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Usuario? Usuario { get; set; }
    public ICollection<ProfissionalEstabelecimento> Estabelecimentos { get; set; } = new List<ProfissionalEstabelecimento>();
    public ICollection<ProfissionalServico> Servicos { get; set; } = new List<ProfissionalServico>();
    public ICollection<HorarioAtendimentoProfissional> HorariosAtendimento { get; set; } = new List<HorarioAtendimentoProfissional>();
    public ICollection<AgendamentoItem> AgendamentoItens { get; set; } = new List<AgendamentoItem>();
    public ICollection<AgendamentoItem> AgendamentoItensRepassados { get; set; } = new List<AgendamentoItem>();
}
