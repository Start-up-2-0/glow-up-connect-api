using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IAvaliacaoConviteRepository : IRepository<AvaliacaoConvite>
{
    Task<AvaliacaoConvite?> ObterPorTokenComAgendamentoAsync(
        Guid tokenPublico,
        CancellationToken cancellationToken = default);

    Task<AvaliacaoConvite?> ObterPorAgendamentoIdAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default);
}
