using System.Text.Json;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IWebhookWhatsAppService
{
    Task ProcessarMensagemRecebidaAsync(JsonElement payload, CancellationToken cancellationToken = default);

    Task ProcessarMensagemEnviadaAsync(JsonElement payload, CancellationToken cancellationToken = default);
}
