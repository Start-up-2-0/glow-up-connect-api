using GLOWAPI.Application.Models.Mensageria;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IConfirmacaoWhatsAppInboundService
{
    Task ProcessarAsync(
        string telefoneRemetente,
        string textoMensagem,
        ConfirmacaoWhatsAppInboundContexto? contextoConversa,
        CancellationToken cancellationToken = default);
}
