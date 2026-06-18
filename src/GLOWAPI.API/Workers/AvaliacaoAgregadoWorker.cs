using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Workers;

public class AvaliacaoAgregadoWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AvaliacaoAgregadoWorkerOptions _options;
    private readonly ILogger<AvaliacaoAgregadoWorker> _logger;

    public AvaliacaoAgregadoWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<AvaliacaoAgregadoWorkerOptions> options,
        ILogger<AvaliacaoAgregadoWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Worker de agregados de avaliacao iniciado. Habilitado={Habilitado}",
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
                var workerService = scope.ServiceProvider.GetRequiredService<IAvaliacaoAgregadoWorkerService>();
                await workerService.ProcessarCicloDiarioAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo do worker de agregados de avaliacao.");
            }

            await Task.Delay(_options.IntervaloProcessamentoMs, stoppingToken);
        }

        _logger.LogInformation("Worker de agregados de avaliacao encerrado.");
    }
}
