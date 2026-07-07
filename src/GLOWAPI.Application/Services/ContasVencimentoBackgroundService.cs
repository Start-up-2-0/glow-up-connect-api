using GLOWAPI.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GLOWAPI.Application.Services;

public class ContasVencimentoBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContasVencimentoBackgroundService> _logger;

    public ContasVencimentoBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ContasVencimentoBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var financeiro = scope.ServiceProvider.GetRequiredService<IFinanceiroNegocioService>();
                await financeiro.AtualizarContasVencidasAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar contas vencidas.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
