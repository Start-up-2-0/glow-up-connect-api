using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Workers;

public class MensagemNotificacaoRecuperacaoWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MensageriaOptions _options;
    private readonly ILogger<MensagemNotificacaoRecuperacaoWorker> _logger;

    public MensagemNotificacaoRecuperacaoWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<MensageriaOptions> options,
        ILogger<MensagemNotificacaoRecuperacaoWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de recuperacao de mensageria iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_options.Habilitado)
            {
                await Task.Delay(_options.IntervaloRecuperacaoMs, stoppingToken);
                continue;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IMensagemNotificacaoRepository>();
                var recuperadas = await repository.RecuperarTravadasAsync(
                    _options.TimeoutProcessamentoMinutos,
                    DateTime.UtcNow,
                    stoppingToken);

                if (recuperadas > 0)
                {
                    _logger.LogWarning(
                        "Mensagens travadas recuperadas. Quantidade={Quantidade}, TimeoutMinutos={TimeoutMinutos}",
                        recuperadas,
                        _options.TimeoutProcessamentoMinutos);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no worker de recuperacao de mensageria.");
            }

            await Task.Delay(_options.IntervaloRecuperacaoMs, stoppingToken);
        }

        _logger.LogInformation("Worker de recuperacao de mensageria encerrado.");
    }
}
