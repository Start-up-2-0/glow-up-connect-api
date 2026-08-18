namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IRetencaoDadosRepository
{
    Task<int> RemoverWebhooksProcessadosAntesDeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    Task<int> RemoverMensagensNotificacaoLogsAntesDeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    Task<int> RemoverMensagensNotificacaoTerminaisAntesDeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    Task<int> RemoverLogsAutenticacaoAntesDeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    Task<int> RemoverSessoesAutenticacaoExpiradasAsync(DateTime utcNow, CancellationToken cancellationToken = default);

    Task<int> AnularPayloadAssinaturasHistoricoAntesDeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    Task<int> AnularPayloadPagamentosHistoricoAntesDeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    Task<int> AnularPayloadAgendamentosHistoricoAntesDeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
