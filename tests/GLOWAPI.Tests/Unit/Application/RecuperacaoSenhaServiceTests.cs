using GLOWAPI.Application.DTOs.Auth;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Usuario;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class RecuperacaoSenhaServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IGlowTokenService> _tokenService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IMensagemNotificacaoService> _mensagemService = new();
    private readonly Mock<IAuthSessionService> _authSessionService = new();
    private readonly AuthOptions _authOptions = new()
    {
        ConfirmacaoCodigoDigitos = 6,
        RecuperacaoSenhaMinutos = 30,
        FrontendBaseUrl = "http://localhost:3000"
    };

    public RecuperacaoSenhaServiceTests()
    {
        _tokenService.Setup(t => t.GerarRefreshToken()).Returns("token-plano-abc");
        _tokenService.Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns<string>(value => $"hash-{value}");
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>()))
            .Returns<string>(value => $"bcrypt-{value}");
    }

    [Fact]
    public async Task SolicitarAsync_DeveRegistrarEmailComTemplateHtml_QuandoUsuarioAtivo()
    {
        var usuario = CriarUsuarioAtivo();
        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("gustavo@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        RegistrarMensagemNotificacaoDto? mensagemRegistrada = null;
        _mensagemService
            .Setup(m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagemRegistrada = dto);

        var service = CreateService();
        await service.SolicitarAsync("  Gustavo@email.com  ");

        Assert.Equal("hash-token-plano-abc", usuario.RecuperacaoTokenHash);
        Assert.NotNull(usuario.RecuperacaoCodigoHash);
        Assert.NotNull(usuario.RecuperacaoExpiraEm);
        Assert.True(usuario.RecuperacaoExpiraEm > DateTime.UtcNow.AddMinutes(29));
        Assert.NotNull(mensagemRegistrada);
        Assert.Equal(CanalMensagemNotificacao.Email, mensagemRegistrada!.Canal);
        Assert.Equal("gustavo@email.com", mensagemRegistrada.Destinatario);
        Assert.Equal("Redefina sua senha", mensagemRegistrada.Assunto);
        Assert.Equal(3, mensagemRegistrada.Prioridade);
        Assert.Contains("<!doctype html>", mensagemRegistrada.Conteudo);
        Assert.Contains("class=\"confirm-code\"", mensagemRegistrada.Conteudo);
        Assert.Contains("Redefinir senha", mensagemRegistrada.Conteudo);
        Assert.Contains("/resetar-senha?token=token-plano-abc", mensagemRegistrada.Conteudo);
        _usuarioRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SolicitarAsync_NaoDeveEnfileirar_QuandoEmailInexistente()
    {
        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("ausente@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var service = CreateService();
        await service.SolicitarAsync("ausente@email.com");

        _mensagemService.Verify(
            m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _usuarioRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SolicitarAsync_NaoDeveEnfileirar_QuandoUsuarioInativo()
    {
        var usuario = CriarUsuarioAtivo();
        usuario.Ativo = false;
        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("inativo@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();
        await service.SolicitarAsync("inativo@email.com");

        _mensagemService.Verify(
            m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RedefinirAsync_DeveTrocarSenhaPorToken_ERevogarSessoes()
    {
        var usuario = CriarUsuarioComRecuperacao();
        _usuarioRepository
            .Setup(r => r.ObterPorRecuperacaoTokenHashAsync("hash-token-plano-abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(h => h.Verify("NovaSenha123!", "hash-antiga")).Returns(false);

        var service = CreateService();
        await service.RedefinirAsync(new ResetPasswordRequestDto
        {
            Token = "token-plano-abc",
            Senha = "NovaSenha123!",
            ConfirmarSenha = "NovaSenha123!"
        });

        Assert.Equal("bcrypt-NovaSenha123!", usuario.Senha);
        Assert.Null(usuario.RecuperacaoTokenHash);
        Assert.Null(usuario.RecuperacaoCodigoHash);
        Assert.Null(usuario.RecuperacaoExpiraEm);
        Assert.Equal(0, usuario.Tentativas);
        Assert.Null(usuario.BloqueadoAte);
        _authSessionService.Verify(
            s => s.RevogarTodasSessoesDoUsuarioAsync(1, It.IsAny<CancellationToken>()),
            Times.Once);
        _usuarioRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RedefinirAsync_DeveTrocarSenhaPorCodigo()
    {
        var usuario = CriarUsuarioComRecuperacao();
        _usuarioRepository
            .Setup(r => r.ObterPorRecuperacaoCodigoHashAsync("hash-482913", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(h => h.Verify("NovaSenha123!", "hash-antiga")).Returns(false);

        var service = CreateService();
        await service.RedefinirAsync(new ResetPasswordRequestDto
        {
            Codigo = "482913",
            Senha = "NovaSenha123!",
            ConfirmarSenha = "NovaSenha123!"
        });

        Assert.Equal("bcrypt-NovaSenha123!", usuario.Senha);
        Assert.Null(usuario.RecuperacaoCodigoHash);
    }

    [Fact]
    public async Task RedefinirAsync_DeveLancarExcecao_QuandoExpirado()
    {
        var usuario = CriarUsuarioComRecuperacao();
        usuario.RecuperacaoExpiraEm = DateTime.UtcNow.AddMinutes(-1);
        _usuarioRepository
            .Setup(r => r.ObterPorRecuperacaoTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();

        await Assert.ThrowsAsync<ResetSenhaInvalidoException>(() =>
            service.RedefinirAsync(new ResetPasswordRequestDto
            {
                Token = "token-plano-abc",
                Senha = "NovaSenha123!",
                ConfirmarSenha = "NovaSenha123!"
            }));
    }

    [Fact]
    public async Task RedefinirAsync_DeveLancarExcecao_QuandoSenhaIgualAAtual()
    {
        var usuario = CriarUsuarioComRecuperacao();
        _usuarioRepository
            .Setup(r => r.ObterPorRecuperacaoTokenHashAsync("hash-token-plano-abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _passwordHasher.Setup(h => h.Verify("NovaSenha123!", "hash-antiga")).Returns(true);

        var service = CreateService();

        var ex = await Assert.ThrowsAsync<ResetSenhaInvalidoException>(() =>
            service.RedefinirAsync(new ResetPasswordRequestDto
            {
                Token = "token-plano-abc",
                Senha = "NovaSenha123!",
                ConfirmarSenha = "NovaSenha123!"
            }));

        Assert.Equal(ResetSenhaInvalidoException.ErrorCode, ex.Code);
        _authSessionService.Verify(
            s => s.RevogarTodasSessoesDoUsuarioAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RedefinirAsync_DeveLancarExcecao_QuandoTokenECodigoAusentes()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ResetSenhaInvalidoException>(() =>
            service.RedefinirAsync(new ResetPasswordRequestDto
            {
                Senha = "NovaSenha123!",
                ConfirmarSenha = "NovaSenha123!"
            }));
    }

    private static Usuario CriarUsuarioAtivo() => new()
    {
        Id = 1,
        Email = "gustavo@email.com",
        Nome = "Gustavo",
        Role = UserRole.Cliente,
        Ativo = true,
        Senha = "hash-antiga"
    };

    private static Usuario CriarUsuarioComRecuperacao() => new()
    {
        Id = 1,
        Email = "test@email.com",
        Nome = "Teste",
        Role = UserRole.Cliente,
        Ativo = true,
        Senha = "hash-antiga",
        Tentativas = 3,
        BloqueadoAte = DateTime.UtcNow.AddMinutes(10),
        RecuperacaoTokenHash = "hash-token-plano-abc",
        RecuperacaoCodigoHash = "hash-482913",
        RecuperacaoExpiraEm = DateTime.UtcNow.AddMinutes(20)
    };

    private RecuperacaoSenhaService CreateService() => new(
        _usuarioRepository.Object,
        _tokenService.Object,
        _passwordHasher.Object,
        _mensagemService.Object,
        _authSessionService.Object,
        Options.Create(_authOptions));
}
