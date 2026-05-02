using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IWebhookPagamentoRepository : IRepository<WebhookPagamento>
{
    Task<WebhookPagamento?> ObterPorEventoAsync(GatewayPagamento gateway, string eventId, CancellationToken cancellationToken = default);
}
