using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class WebhookPagamentoRepository : Repository<WebhookPagamento>, IWebhookPagamentoRepository
{
    public WebhookPagamentoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<WebhookPagamento?> ObterPorEventoAsync(GatewayPagamento gateway, string eventId, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            webhook => webhook.Gateway == gateway && webhook.EventId == eventId,
            cancellationToken);
    }
}
