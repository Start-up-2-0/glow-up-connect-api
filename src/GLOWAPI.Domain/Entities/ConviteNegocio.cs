using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class ConviteNegocio
{
    public int Id { get; set; }
    public int EstabelecimentoId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string NomePublico { get; set; } = string.Empty;
    public TipoConviteNegocio TipoConvite { get; set; }
    public EstablishmentUserRole RoleSugerida { get; set; } = EstablishmentUserRole.Profissional;
    public bool PodeReceberAgendamento { get; set; } = true;
    public StatusConviteNegocio Status { get; set; } = StatusConviteNegocio.Pendente;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public int CriadoPorUsuarioId { get; set; }
    public int? AceitoPorUsuarioId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? RespondidoEm { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Estabelecimento? Estabelecimento { get; set; }
    public Usuario? CriadoPorUsuario { get; set; }
    public Usuario? AceitoPorUsuario { get; set; }
}
