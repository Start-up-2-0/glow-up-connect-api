using GLOWAPI.Application.Models.Mensageria;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IConfirmacaoWhatsAppNotificacaoService
{
    Task EnfileirarRespostaProcessandoAsync(
        string telefoneDestino,
        string? nomeDestinatario,
        ConfirmacaoWhatsAppInboundContexto? contextoConversa,
        CancellationToken cancellationToken = default);

    Task EnfileirarRespostaConfirmacaoSucessoAsync(
        WhatsAppConfirmacaoInboundResultado resultado,
        ConfirmacaoWhatsAppInboundContexto? contextoConversa,
        CancellationToken cancellationToken = default);

    Task EnfileirarRespostaJaConfirmadoAsync(
        WhatsAppConfirmacaoInboundResultado resultado,
        ConfirmacaoWhatsAppInboundContexto? contextoConversa,
        CancellationToken cancellationToken = default);

    Task EnfileirarRespostaFalhaAsync(
        WhatsAppConfirmacaoInboundResultado resultado,
        string? telefoneFallback,
        ConfirmacaoWhatsAppInboundContexto? contextoConversa,
        CancellationToken cancellationToken = default);
}
