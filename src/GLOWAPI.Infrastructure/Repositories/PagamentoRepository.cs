using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class PagamentoRepository : Repository<Pagamento>, IPagamentoRepository
{
    public PagamentoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Pagamento?> ObterPorGatewayPaymentIdAsync(
        GatewayPagamento gateway,
        string gatewayPaymentId,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(pagamento => pagamento.Assinatura)
                .ThenInclude(assinatura => assinatura!.Plano)
            .Include(pagamento => pagamento.Assinatura)
                .ThenInclude(assinatura => assinatura!.PlanoAlteracaoPendente)
            .FirstOrDefaultAsync(
                pagamento => pagamento.Gateway == gateway
                    && pagamento.GatewayPaymentId == gatewayPaymentId,
                cancellationToken);
    }
}
