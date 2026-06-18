namespace GLOWAPI.Application.Interfaces.Services;

public interface IAgendamentoConfirmacaoContaService
{
    Task ProcessarConfirmacaoContaClienteAsync(int usuarioId, CancellationToken cancellationToken = default);
}
