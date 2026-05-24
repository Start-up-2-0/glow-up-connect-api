namespace GLOWAPI.Domain.Entities;

public class LogAutenticacao
{
    public int Id { get; set; }
    public int? UsuarioId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Evento { get; set; } = string.Empty;
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public string? Detalhes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
