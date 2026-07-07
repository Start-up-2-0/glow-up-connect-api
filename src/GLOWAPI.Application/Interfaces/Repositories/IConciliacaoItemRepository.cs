using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IConciliacaoItemRepository : IRepository<ConciliacaoItem>
{
    Task<IReadOnlyList<ConciliacaoItem>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        bool? conciliado,
        CancellationToken cancellationToken = default);
}
