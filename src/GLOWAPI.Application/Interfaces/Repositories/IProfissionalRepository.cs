using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IProfissionalRepository : IRepository<Profissional>
{
    Task<Profissional?> ObterPorPublicGuidAsync(Guid publicGuid, CancellationToken cancellationToken = default);
    Task<Profissional?> ObterPorUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<Profissional?> ObterPorIdComEnderecoAsync(int id, CancellationToken cancellationToken = default);
}
