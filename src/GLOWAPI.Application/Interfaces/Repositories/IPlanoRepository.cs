using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IPlanoRepository : IRepository<Plano>
{
    Task<IReadOnlyList<Plano>> ListarAtivosAsync(CancellationToken cancellationToken = default);
}
