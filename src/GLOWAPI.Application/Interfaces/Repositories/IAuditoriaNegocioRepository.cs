using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IAuditoriaNegocioRepository : IRepository<AuditoriaNegocio>
{
    Task<IReadOnlyList<AuditoriaNegocio>> ListarRecentesPorEstabelecimentoAsync(
        int estabelecimentoId,
        int limite,
        CancellationToken cancellationToken = default);
}
