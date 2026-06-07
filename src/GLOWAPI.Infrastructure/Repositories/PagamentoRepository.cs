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

    public async Task<IReadOnlyList<Pagamento>> ListarPorAssinaturaAsync(
        int assinaturaId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Where(pagamento => pagamento.AssinaturaId == assinaturaId)
            .OrderByDescending(pagamento => pagamento.CreateAd)
            .ThenByDescending(pagamento => pagamento.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Pagamento>> ListarPendentesVencidosAsync(
        DateTime dataReferenciaUtc,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(pagamento => pagamento.Assinatura)
            .Where(pagamento =>
                pagamento.AssinaturaId != null
                && pagamento.Status == PagamentoStatus.Pendente
                && pagamento.DataVencimento.HasValue
                && pagamento.DataVencimento.Value.Date < dataReferenciaUtc.Date)
            .ToListAsync(cancellationToken);
}
