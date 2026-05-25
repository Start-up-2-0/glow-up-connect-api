namespace GLOWAPI.Application.Interfaces.Services;

public interface IMensagemNotificacaoProcessadorService
{
    Task<int> ProcessarLoteAsync(
        string instanciaWorker,
        CancellationToken cancellationToken = default);
}
