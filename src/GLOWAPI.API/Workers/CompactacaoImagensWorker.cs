using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Workers;

public class CompactacaoImagensWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CompactacaoImagensWorkerOptions _options;
    private readonly ILogger<CompactacaoImagensWorker> _logger;
    private readonly CompactacaoImagensCursor _cursor = new();
    private bool _cicloCompleto;

    public CompactacaoImagensWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<CompactacaoImagensWorkerOptions> options,
        ILogger<CompactacaoImagensWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Worker de compactacao de imagens iniciado. Habilitado={Habilitado}",
            _options.Habilitado);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_options.Habilitado || _cicloCompleto)
            {
                await Task.Delay(_options.IntervaloProcessamentoMs, stoppingToken);
                continue;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ICompactacaoImagensPersistidasService>();
                var resultado = await service.ProcessarLoteAsync(
                    _cursor,
                    Math.Max(1, _options.TamanhoLote),
                    stoppingToken);

                if (resultado.RegistrosProcessados == 0)
                {
                    _cicloCompleto = true;
                    _logger.LogInformation("Worker de compactacao de imagens concluiu varredura completa.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo do worker de compactacao de imagens.");
            }

            await Task.Delay(_options.IntervaloProcessamentoMs, stoppingToken);
        }

        _logger.LogInformation("Worker de compactacao de imagens encerrado.");
    }
}
