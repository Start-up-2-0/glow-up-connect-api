using System.Text.Json;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IWebhookWhatsAppService
{
    Task ProcessarEvolutionWebhookAsync(JsonElement payload, CancellationToken cancellationToken = default);
}
