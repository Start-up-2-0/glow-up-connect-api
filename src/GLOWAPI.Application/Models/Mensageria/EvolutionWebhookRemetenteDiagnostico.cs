namespace GLOWAPI.Application.Models.Mensageria;

public record EvolutionWebhookRemetenteDiagnostico(
    string? RemoteJid,
    bool EhLid,
    bool RemoteJidAltPresente,
    bool SenderPnKeyPresente,
    bool SenderPnDataPresente,
    bool ParticipantPresente,
    string? SenderInstancia,
    string? PushName,
    bool? FromMe,
    string TelefoneExtraido,
    string? MotivoTelefoneVazio);
