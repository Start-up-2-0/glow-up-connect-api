using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class MensagemNotificacaoLog
{
    public int Id { get; set; }
    public int MensagemNotificacaoId { get; set; }
    public int Tentativa { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public string? RespostaProvedor { get; set; }
    public int TempoExecucaoMs { get; set; }
    public StatusMensagemNotificacao Status { get; set; }
    public string? MensagemErro { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public MensagemNotificacao MensagemNotificacao { get; set; } = null!;
}
