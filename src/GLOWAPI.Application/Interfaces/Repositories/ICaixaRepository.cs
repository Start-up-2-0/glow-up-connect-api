using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface ICaixaRepository : IRepository<Caixa>
{
    Task<Caixa?> ObterPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<Caixa?> ObterPorEstabelecimentoComTrackingAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<Caixa> ObterOuProvisionarPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<Caixa> ObterOuProvisionarPorEstabelecimentoComTrackingAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);
}
