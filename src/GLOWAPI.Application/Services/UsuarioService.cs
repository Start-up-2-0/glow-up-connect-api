using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UsuarioService(IUsuarioRepository usuarioRepository, IPasswordHasher passwordHasher)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Usuario> CriarUsuarioAsync(string nome, string email, string telefone, string senha, UserRole role, CancellationToken cancellationToken = default)
    {
        var usuarioExistente = await _usuarioRepository.ObterPorEmailAsync(email, cancellationToken);
        if (usuarioExistente != null)
        {
            throw new InvalidOperationException("Usuário com este email já existe.");
        }

        var senhaHash = _passwordHasher.Hash(senha);

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            Telefone = telefone,
            Senha = senhaHash,
            Role = role,
            Ativo = true
        };

        await _usuarioRepository.AdicionarAsync(usuario, cancellationToken);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        return usuario;
    }

    public async Task<Usuario?> ObterUsuarioPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _usuarioRepository.ObterPorIdAsync(id, cancellationToken);
    }

    public async Task<Usuario?> ObterUsuarioPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _usuarioRepository.ObterPorEmailAsync(email, cancellationToken);
    }

    public async Task AtualizarUsuarioAsync(int id, string nome, string telefone, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id, cancellationToken);
        if (usuario == null || !usuario.Ativo)
        {
            throw new KeyNotFoundException("Usuário não encontrado ou inativo.");
        }

        usuario.Nome = nome;
        usuario.Telefone = telefone;
        usuario.UpdatedAt = DateTime.UtcNow;

        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task DesativarUsuarioAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id, cancellationToken);
        if (usuario == null)
        {
            throw new KeyNotFoundException("Usuário não encontrado.");
        }

        usuario.Ativo = false;
        usuario.UpdatedAt = DateTime.UtcNow;

        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    public bool VerificarSenha(string senha, string senhaHash) =>
        _passwordHasher.Verify(senha, senhaHash);
}