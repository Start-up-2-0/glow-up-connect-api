using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Workers;

public class AssinaturaCobrancaWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AssinaturaCobrancaWorkerOptions _options;
    private readonly ILogger<AssinaturaCobrancaWorker> _logger;

    public AssinaturaCobrancaWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<AssinaturaCobrancaWorkerOptions> options,
        ILogger<AssinaturaCobrancaWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Worker de cobranca de assinatura iniciado. Habilitado={Habilitado}",
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
                var workerService = scope.ServiceProvider.GetRequiredService<IAssinaturaCobrancaWorkerService>();
                await workerService.ProcessarCicloDiarioAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo do worker de cobranca de assinatura.");
            }

            await Task.Delay(_options.IntervaloProcessamentoMs, stoppingToken);
        }

        _logger.LogInformation("Worker de cobranca de assinatura encerrado.");
    }
}
