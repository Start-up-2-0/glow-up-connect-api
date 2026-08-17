using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class ConviteNegocio
{
    public int Id { get; set; }
    public int EstabelecimentoId { get; set; }
    /// <summary>Legado: convites antigos por e-mail. Links multi-uso ficam vazios.</summary>
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string NomePublico { get; set; } = string.Empty;
    public TipoConviteNegocio TipoConvite { get; set; }
    public EstablishmentUserRole RoleSugerida { get; set; } = EstablishmentUserRole.Profissional;
    public bool PodeReceberAgendamento { get; set; } = true;
    public StatusConviteNegocio Status { get; set; } = StatusConviteNegocio.Ativo;
    public string TokenHash { get; set; } = string.Empty;
    /// <summary>Token do link cifrado (AES-GCM) para recuperação por quem gerencia a equipe.</summary>
    public string? TokenProtegido { get; set; }
    public int LimiteUsuarios { get; set; } = 1;
    public int QuantidadeUtilizacoes { get; set; }
    public DateTime ExpiraEm { get; set; }
    public int CriadoPorUsuarioId { get; set; }
    public int? AceitoPorUsuarioId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? RespondidoEm { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Estabelecimento? Estabelecimento { get; set; }
    public Usuario? CriadoPorUsuario { get; set; }
    public Usuario? AceitoPorUsuario { get; set; }
    public ICollection<ConviteNegocioUtilizacao> Utilizacoes { get; set; } = new List<ConviteNegocioUtilizacao>();
}
