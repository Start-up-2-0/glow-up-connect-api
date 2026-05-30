using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class AuditoriaNegocio
{
    public int Id { get; set; }
    public int EstabelecimentoId { get; set; }
    public int? UsuarioId { get; set; }
    public TipoAcaoAuditoriaNegocio TipoAcao { get; set; }
    public string Entidade { get; set; } = string.Empty;
    public int? EntidadeId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Estabelecimento? Estabelecimento { get; set; }
    public Usuario? Usuario { get; set; }
}
