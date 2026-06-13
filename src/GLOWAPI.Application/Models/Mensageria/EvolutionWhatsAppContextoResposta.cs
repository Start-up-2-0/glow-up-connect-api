namespace GLOWAPI.Application.Models.Mensageria;

public sealed record EvolutionWhatsAppContextoResposta(
    string? MessageId,
    string? RemoteJidConversa,
    bool? FromMe,
    string? TextoMensagemReferencia)
{
    public bool TemQuoted =>
        !string.IsNullOrWhiteSpace(MessageId)
        && !string.IsNullOrWhiteSpace(RemoteJidConversa)
        && !string.IsNullOrWhiteSpace(TextoMensagemReferencia);
}
