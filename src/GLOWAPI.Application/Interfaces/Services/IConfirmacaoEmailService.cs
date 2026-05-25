using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IConfirmacaoEmailService
{
    Task<(string TokenPlano, string CodigoPlano)> GerarEEnviarConfirmacaoAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task ConfirmarPorTokenAsync(string token, CancellationToken cancellationToken = default);
    Task ConfirmarPorCodigoAsync(string codigo, CancellationToken cancellationToken = default);
    Task ReenviarConfirmacaoAsync(string email, CancellationToken cancellationToken = default);
}
