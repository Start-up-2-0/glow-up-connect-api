using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.DTOs.Usuario;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Usuario;

namespace GLOWAPI.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuthSessionService _authSessionService;
    private readonly IConfirmacaoEmailService _confirmacaoEmailService;
    private readonly IConfirmacaoWhatsAppService _confirmacaoWhatsAppService;
    private readonly IAvatarBase64Decoder _avatarDecoder;

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserContext currentUser,
        IAuthSessionService authSessionService,
        IConfirmacaoEmailService confirmacaoEmailService,
        IConfirmacaoWhatsAppService confirmacaoWhatsAppService,
        IAvatarBase64Decoder avatarDecoder)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _authSessionService = authSessionService;
        _confirmacaoEmailService = confirmacaoEmailService;
        _confirmacaoWhatsAppService = confirmacaoWhatsAppService;
        _avatarDecoder = avatarDecoder;
    }

    public async Task<Usuario> CadastrarClienteAsync(
        CadastrarClienteDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = ConfirmacaoEmailService.NormalizarEmail(dto.Email);
        var usuarioExistente = await _usuarioRepository.ObterPorEmailAsync(email, cancellationToken);
        if (usuarioExistente is not null)
        {
            throw new EmailJaCadastradoException();
        }

        var senhaHash = _passwordHasher.Hash(dto.Senha);

        string? avatarBase64 = null;
        if (!string.IsNullOrWhiteSpace(dto.AvatarBase64))
        {
            avatarBase64 = _avatarDecoder.ValidarENormalizar(dto.AvatarBase64, dto.AvatarContentType);
        }

        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = email,
            Telefone = dto.Telefone.Trim(),
            Senha = senhaHash,
            Role = UserRole.Cliente,
            Ativo = false,
            AvatarBase64 = avatarBase64
        };

        await _usuarioRepository.AdicionarAsync(usuario, cancellationToken);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        await _confirmacaoEmailService.GerarEEnviarConfirmacaoAsync(usuario, cancellationToken);

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

    public async Task<Usuario> ObterPerfilAtualAsync(CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        return await ObterUsuarioAtivoAsync(userId, cancellationToken);
    }

    public async Task AtualizarPerfilAtualAsync(
        AtualizarUsuarioDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var usuario = await ObterUsuarioAtivoAsync(userId, cancellationToken);
        var telefoneAnterior = usuario.Telefone;

        usuario.Nome = dto.Nome;
        usuario.Telefone = dto.Telefone.Trim();
        usuario.UpdatedAt = DateTime.UtcNow;

        WhatsAppConfirmacaoEntidade.ResetarAoAlterarTelefone(
            telefoneAnterior,
            usuario.Telefone,
            () =>
            {
                usuario.WhatsAppConfirmadoEm = null;
                usuario.WhatsAppOptIn = false;
                usuario.LimparConfirmacaoWhatsApp();
            });

        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        if (!string.Equals(telefoneAnterior, usuario.Telefone, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(usuario.Telefone))
        {
            await _confirmacaoWhatsAppService.IniciarConfirmacaoAsync(usuario, cancellationToken);
        }
    }

    public async Task<WhatsAppConfirmacaoInstrucoesDto> SolicitarConfirmacaoWhatsAppAtualAsync(CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var usuario = await ObterUsuarioAtivoAsync(userId, cancellationToken);

        if (usuario.WhatsAppConfirmadoEm.HasValue)
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        return await _confirmacaoWhatsAppService.IniciarConfirmacaoAsync(usuario, cancellationToken);
    }

    public async Task AtualizarWhatsAppOptInAtualAsync(bool optIn, CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var usuario = await ObterUsuarioAtivoAsync(userId, cancellationToken);

        if (!usuario.WhatsAppConfirmadoEm.HasValue)
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        usuario.WhatsAppOptIn = optIn;
        usuario.UpdatedAt = DateTime.UtcNow;

        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task DesativarContaAtualAsync(CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var sessionId = ObterSessionIdAutenticado();

        var usuario = await _usuarioRepository.ObterPorIdAsync(userId, cancellationToken);
        if (usuario is null)
        {
            throw new UsuarioNaoEncontradoException();
        }

        usuario.Ativo = false;
        usuario.LimparConfirmacaoEmail();
        usuario.UpdatedAt = DateTime.UtcNow;

        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        var sessao = await _authSessionService.ObterSessaoPorIdAsync(sessionId, cancellationToken);
        if (sessao is not null)
        {
            await _authSessionService.RevogarSessaoAsync(sessao, cancellationToken);
        }
    }

    public bool VerificarSenha(string senha, string senhaHash) =>
        _passwordHasher.Verify(senha, senhaHash);

    private int ObterUserIdAutenticado()
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        return _currentUser.UserId.Value;
    }

    private int ObterSessionIdAutenticado()
    {
        if (!_currentUser.SessionId.HasValue)
        {
            throw new UnauthorizedException();
        }

        return _currentUser.SessionId.Value;
    }

    private async Task<Usuario> ObterUsuarioAtivoAsync(int id, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id, cancellationToken);
        if (usuario is null || !usuario.Ativo)
        {
            throw new UsuarioNaoEncontradoException();
        }

        return usuario;
    }
}
