namespace GLOWAPI.Application.Models.Mensageria;

public record WhatsAppConfirmacaoInboundRespostaDestino(
    string Telefone,
    string? Nome,
    string? RemoteJidConversa = null)
{
    public string DestinatarioEvolution =>
        !string.IsNullOrWhiteSpace(RemoteJidConversa) ? RemoteJidConversa : Telefone;
}
