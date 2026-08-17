using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Workers;

public class RetencaoDadosWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RetencaoDadosOptions _options;
    private readonly ILogger<RetencaoDadosWorker> _logger;

    public RetencaoDadosWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<RetencaoDadosOptions> options,
        ILogger<RetencaoDadosWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Worker de retencao de dados iniciado. Habilitado={Habilitado}, DiasRetencao={DiasRetencao}",
            _options.Habilitado,
            _options.DiasRetencao);

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
                var service = scope.ServiceProvider.GetRequiredService<IRetencaoDadosService>();
                await service.ExecutarAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo do worker de retencao de dados.");
            }

            await Task.Delay(_options.IntervaloProcessamentoMs, stoppingToken);
        }

        _logger.LogInformation("Worker de retencao de dados encerrado.");
    }
}
