using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IMetaRepository : IRepository<Meta>
{
    Task<IReadOnlyList<Meta>> ListarAtivasPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<Meta?> ObterPorIdComTrackingAsync(
        int metaId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteMetaAtivaNoPeriodoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);
}
