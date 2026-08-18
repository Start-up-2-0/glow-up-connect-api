using GLOWAPI.Application.DTOs.Auth;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IRecuperacaoSenhaService
{
    Task SolicitarAsync(string email, CancellationToken cancellationToken = default);

    Task RedefinirAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
}
