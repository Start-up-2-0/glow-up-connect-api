using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IPagamentoRepository : IRepository<Pagamento>
{
    Task<Pagamento?> ObterPorGatewayPaymentIdAsync(
        GLOWAPI.Domain.Enums.GatewayPagamento gateway,
        string gatewayPaymentId,
        CancellationToken cancellationToken = default);

    Task<Pagamento?> ObterPorReferenciaInternaAsync(
        string referenciaInterna,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pagamento>> ListarPorAssinaturaAsync(int assinaturaId, CancellationToken cancellationToken = default);

    Task<Pagamento?> ObterUltimoPendenteInicialPorAssinaturaAsync(
        int assinaturaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pagamento>> ListarPendentesVencidosAsync(DateTime dataReferenciaUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pagamento>> ListarAtrasadosAlemToleranciaAsync(
        DateTime dataLimiteVencimento,
        CancellationToken cancellationToken = default);

    Task<Pagamento?> ObterPagoPorAgendamentoAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default);
}
