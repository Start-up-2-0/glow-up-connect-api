using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IAgendamentoHistoricoRepository : IRepository<AgendamentoHistorico>
{
    Task<IReadOnlyList<AgendamentoHistorico>> ListarPorAgendamentoAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default);
}
