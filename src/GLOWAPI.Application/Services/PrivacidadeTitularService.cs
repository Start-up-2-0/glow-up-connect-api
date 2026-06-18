using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;

namespace GLOWAPI.Application.Services;

public class PrivacidadeTitularService : IPrivacidadeTitularService
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUsuarioRepository _usuarioRepository;

    public PrivacidadeTitularService(
        ICurrentUserContext currentUserContext,
        IUsuarioRepository usuarioRepository)
    {
        _currentUserContext = currentUserContext;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<object> ExportarMeusDadosAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated || !_currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedAccessException();
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(_currentUserContext.UserId.Value, cancellationToken);
        if (usuario is null)
        {
            throw new KeyNotFoundException();
        }

        return new
        {
            exportadoEm = DateTime.UtcNow,
            usuario = new
            {
                usuario.Id,
                usuario.Nome,
                usuario.Email,
                usuario.Telefone,
                usuario.Role,
                usuario.Ativo,
                usuario.CreatedAt
            }
        };
    }

    public Task SolicitarExclusaoAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            throw new UnauthorizedAccessException();
        }

        return Task.CompletedTask;
    }

    public Task RevogarConsentimentoAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            throw new UnauthorizedAccessException();
        }

        return Task.CompletedTask;
    }
}
