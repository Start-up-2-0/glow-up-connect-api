using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface ICampanhaPromocionalRepository : IRepository<CampanhaPromocional>
{
    Task<CampanhaPromocional?> ObterAtivaPorCodigoAsync(string codigo, CancellationToken cancellationToken = default);
    Task<bool> TentarReservarVagaAsync(string codigo, CancellationToken cancellationToken = default);
}
