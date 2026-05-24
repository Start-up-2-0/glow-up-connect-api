using GLOWAPI.Application.DTOs.Usuario;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IUsuarioService
{
    Task<Usuario> CriarUsuarioAsync(CriarUsuarioDto dto, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterUsuarioPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterUsuarioPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Usuario> ObterPerfilAtualAsync(CancellationToken cancellationToken = default);
    Task AtualizarPerfilAtualAsync(AtualizarUsuarioDto dto, CancellationToken cancellationToken = default);
    Task DesativarContaAtualAsync(CancellationToken cancellationToken = default);
    bool VerificarSenha(string senha, string Senha);
}
