using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IPagamentoRepository : IRepository<Pagamento>
{
    Task<Pagamento?> ObterPorGatewayPaymentIdAsync(
        GLOWAPI.Domain.Enums.GatewayPagamento gateway,
        string gatewayPaymentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pagamento>> ListarPorAssinaturaAsync(int assinaturaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pagamento>> ListarPendentesVencidosAsync(DateTime dataReferenciaUtc, CancellationToken cancellationToken = default);
}
