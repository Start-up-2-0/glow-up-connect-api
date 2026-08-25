using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IComodidadeRepository : IRepository<Comodidade>
{
    Task<IReadOnlyList<Comodidade>> ListarAtivasAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Comodidade>> ListarPorEstabelecimentoAsync(int estabelecimentoId, CancellationToken cancellationToken = default);
    Task SubstituirDoEstabelecimentoAsync(int estabelecimentoId, IReadOnlyCollection<int> comodidadeIds, CancellationToken cancellationToken = default);
}
