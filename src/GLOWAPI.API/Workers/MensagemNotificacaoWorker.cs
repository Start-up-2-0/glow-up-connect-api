using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Workers;

public class MensagemNotificacaoWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MensageriaOptions _options;
    private readonly ILogger<MensagemNotificacaoWorker> _logger;
    private readonly string _instanciaWorker;

    public MensagemNotificacaoWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<MensageriaOptions> options,
        ILogger<MensagemNotificacaoWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
        _instanciaWorker = $"{_options.InstanciaWorkerPrefixo}-{Environment.MachineName}-{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Worker de mensageria iniciado. InstanciaWorker={InstanciaWorker}, Habilitado={Habilitado}",
            _instanciaWorker,
            _options.Habilitado);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_options.Habilitado)
            {
                await Task.Delay(_options.IntervaloProcessamentoMs, stoppingToken);
                continue;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processador = scope.ServiceProvider.GetRequiredService<IMensagemNotificacaoProcessadorService>();
                var processadas = await processador.ProcessarLoteAsync(_instanciaWorker, stoppingToken);

                if (processadas > 0)
                {
                    _logger.LogInformation(
                        "Lote processado. Quantidade={Quantidade}, InstanciaWorker={InstanciaWorker}",
                        processadas,
                        _instanciaWorker);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro no ciclo do worker de mensageria. InstanciaWorker={InstanciaWorker}",
                    _instanciaWorker);
            }

            await Task.Delay(_options.IntervaloProcessamentoMs, stoppingToken);
        }

        _logger.LogInformation("Worker de mensageria encerrado. InstanciaWorker={InstanciaWorker}", _instanciaWorker);
    }
}
