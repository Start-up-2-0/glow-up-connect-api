namespace GLOWAPI.Domain.Entities;

public class SessaoAutenticacao
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string AccessTokenHash { get; set; } = string.Empty;
    public string RefreshTokenHash { get; set; } = string.Empty;
    public DateTime LoginEm { get; set; } = DateTime.UtcNow;
    public DateTime AccessTokenExpiraEm { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime? UltimaRenovacaoEm { get; set; }
    public DateTime? RevogadoEm { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Usuario Usuario { get; set; } = null!;

    public bool EstaAtiva(DateTime utcNow) =>
        RevogadoEm is null && ExpiraEm > utcNow;

    public void Revogar(DateTime utcNow)
    {
        RevogadoEm = utcNow;
    }

    public void RenovarExpiracao(DateTime utcNow, DateTime novaExpiracaoRefresh)
    {
        UltimaRenovacaoEm = utcNow;
        ExpiraEm = novaExpiracaoRefresh;
    }
}
