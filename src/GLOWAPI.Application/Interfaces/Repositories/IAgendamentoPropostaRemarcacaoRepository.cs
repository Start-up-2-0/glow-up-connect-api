using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IAgendamentoPropostaRemarcacaoRepository : IRepository<AgendamentoPropostaRemarcacao>
{
    Task<AgendamentoPropostaRemarcacao?> ObterPorTokenComAgendamentoAsync(
        Guid tokenPublico,
        CancellationToken cancellationToken = default);

    Task<AgendamentoPropostaRemarcacao?> ObterPendentePorAgendamentoAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default);

    Task<AgendamentoPropostaRemarcacao?> ObterPorIdEAgendamentoAsync(
        int propostaId,
        int agendamentoId,
        CancellationToken cancellationToken = default);

    Task ExpirarPendentesAnterioresAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default);
}
