using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IUsuarioService
{
    Task<Usuario> CriarUsuarioAsync(string nome, string email, string telefone, string senha, UserRole role, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterUsuarioPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterUsuarioPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AtualizarUsuarioAsync(int id, string nome, string telefone, CancellationToken cancellationToken = default);
    Task DesativarUsuarioAsync(int id, CancellationToken cancellationToken = default);
    bool VerificarSenha(string senha, string senhaHash);
}