using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Manutencao;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class RetencaoDadosService : IRetencaoDadosService
{
    private readonly IRetencaoDadosRepository _repository;
    private readonly RetencaoDadosOptions _options;
    private readonly ILogger<RetencaoDadosService> _logger;

    public RetencaoDadosService(
        IRetencaoDadosRepository repository,
        IOptions<RetencaoDadosOptions> options,
        ILogger<RetencaoDadosService> logger)
    {
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RetencaoDadosResultado> ExecutarAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var cutoffUtc = utcNow.AddDays(-Math.Max(1, _options.DiasRetencao));
        var resultado = new RetencaoDadosResultado();

        resultado.MensagensNotificacaoLogsRemovidos =
            await _repository.RemoverMensagensNotificacaoLogsAntesDeAsync(cutoffUtc, cancellationToken);

        resultado.MensagensNotificacaoRemovidas =
            await _repository.RemoverMensagensNotificacaoTerminaisAntesDeAsync(cutoffUtc, cancellationToken);

        resultado.WebhooksRemovidos =
            await _repository.RemoverWebhooksProcessadosAntesDeAsync(cutoffUtc, cancellationToken);

        resultado.LogsAutenticacaoRemovidos =
            await _repository.RemoverLogsAutenticacaoAntesDeAsync(cutoffUtc, cancellationToken);

        resultado.SessoesAutenticacaoRemovidas =
            await _repository.RemoverSessoesAutenticacaoExpiradasAsync(utcNow, cancellationToken);

        resultado.AssinaturasHistoricoPayloadsAnulados =
            await _repository.AnularPayloadAssinaturasHistoricoAntesDeAsync(cutoffUtc, cancellationToken);

        resultado.PagamentosHistoricoPayloadsAnulados =
            await _repository.AnularPayloadPagamentosHistoricoAntesDeAsync(cutoffUtc, cancellationToken);

        resultado.AgendamentosHistoricoPayloadsAnulados =
            await _repository.AnularPayloadAgendamentosHistoricoAntesDeAsync(cutoffUtc, cancellationToken);

        if (resultado.TotalAfetados > 0)
        {
            _logger.LogInformation(
                "Retencao de dados concluida (cutoff={CutoffUtc:O}): webhooks={Webhooks}, logsNotificacao={LogsNotificacao}, mensagens={Mensagens}, logsAuth={LogsAuth}, sessoes={Sessoes}, payloadsAssinatura={PayloadsAssinatura}, payloadsPagamento={PayloadsPagamento}, payloadsAgendamento={PayloadsAgendamento}",
                cutoffUtc,
                resultado.WebhooksRemovidos,
                resultado.MensagensNotificacaoLogsRemovidos,
                resultado.MensagensNotificacaoRemovidas,
                resultado.LogsAutenticacaoRemovidos,
                resultado.SessoesAutenticacaoRemovidas,
                resultado.AssinaturasHistoricoPayloadsAnulados,
                resultado.PagamentosHistoricoPayloadsAnulados,
                resultado.AgendamentosHistoricoPayloadsAnulados);
        }

        return resultado;
    }
}
