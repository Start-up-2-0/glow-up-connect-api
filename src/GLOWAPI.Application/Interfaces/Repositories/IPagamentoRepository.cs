using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IPagamentoRepository : IRepository<Pagamento>
{
    Task<Pagamento?> ObterPorGatewayPaymentIdAsync(
        GLOWAPI.Domain.Enums.GatewayPagamento gateway,
        string gatewayPaymentId,
        CancellationToken cancellationToken = default);
}
