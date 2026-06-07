namespace GLOWAPI.Domain.Entities;

public class RecuperacaoSenha
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string CodigoHash { get; set; } = string.Empty;
    public DateTime CodigoExpiraEm { get; set; }
    public bool CodigoVerificado { get; set; }
    public int CodigoTentativas { get; set; }
    public string? ResetTokenHash { get; set; }
    public DateTime? ResetTokenExpiraEm { get; set; }
    public bool ResetTokenConsumido { get; set; }
    public string? IpSolicitacao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }

    // Navigation
    public Usuario Usuario { get; set; } = null!;
}
