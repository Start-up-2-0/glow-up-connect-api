using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IMensagemNotificacaoRepository : IRepository<MensagemNotificacao>
{
    Task<MensagemNotificacao?> ObterPorGuidAsync(Guid guid, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MensagemNotificacao>> ReservarLoteAsync(
        int batchSize,
        string instanciaWorker,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<int> RecuperarTravadasAsync(
        int timeoutMinutos,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task AdicionarLogAsync(MensagemNotificacaoLog log, CancellationToken cancellationToken = default);
}
