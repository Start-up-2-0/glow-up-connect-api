using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Usuario;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ConfirmacaoEmailServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IGlowTokenService> _tokenService = new();
    private readonly Mock<IMensagemNotificacaoService> _mensagemService = new();
    private readonly Mock<IConfirmacaoWhatsAppService> _confirmacaoWhatsAppService = new();
    private readonly AuthOptions _authOptions = new()
    {
        ConfirmacaoEmailHoras = 24,
        ConfirmacaoCodigoDigitos = 6,
        FrontendBaseUrl = "http://localhost:3000"
    };

    public ConfirmacaoEmailServiceTests()
    {
        _tokenService.Setup(t => t.GerarRefreshToken()).Returns("token-plano-abc");
        _tokenService.Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns<string>(value => $"hash-{value}");
    }

    [Fact]
    public async Task ConfirmarPorCodigoAsync_DeveAtivarUsuario_QuandoCodigoValido()
    {
        var usuario = CriarUsuarioPendente();
        _usuarioRepository
            .Setup(r => r.ObterPorConfirmacaoCodigoHashAsync("hash-482913", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();
        await service.ConfirmarPorCodigoAsync("482913");

        Assert.True(usuario.Ativo);
        Assert.Null(usuario.ConfirmacaoTokenHash);
        _usuarioRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _confirmacaoWhatsAppService.Verify(
            s => s.IniciarConfirmacaoAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConfirmarPorCodigoAsync_DeveIniciarConfirmacaoWhatsApp_QuandoTelefonePendente()
    {
        var usuario = CriarUsuarioPendente();
        usuario.Telefone = "11988887777";
        _usuarioRepository
            .Setup(r => r.ObterPorConfirmacaoCodigoHashAsync("hash-482913", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();
        await service.ConfirmarPorCodigoAsync("482913");

        _confirmacaoWhatsAppService.Verify(
            s => s.IniciarConfirmacaoAsync(usuario, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmarPorCodigoAsync_NaoDeveIniciarWhatsApp_QuandoJaConfirmado()
    {
        var usuario = CriarUsuarioPendente();
        usuario.Telefone = "11988887777";
        usuario.WhatsAppConfirmadoEm = DateTime.UtcNow;
        _usuarioRepository
            .Setup(r => r.ObterPorConfirmacaoCodigoHashAsync("hash-482913", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();
        await service.ConfirmarPorCodigoAsync("482913");

        _confirmacaoWhatsAppService.Verify(
            s => s.IniciarConfirmacaoAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConfirmarPorCodigoAsync_DeveAtivarUsuario_QuandoInicioWhatsAppFalhar()
    {
        var usuario = CriarUsuarioPendente();
        usuario.Telefone = "11988887777";
        _usuarioRepository
            .Setup(r => r.ObterPorConfirmacaoCodigoHashAsync("hash-482913", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _confirmacaoWhatsAppService
            .Setup(s => s.IniciarConfirmacaoAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConfirmacaoWhatsAppInvalidaException());

        var service = CreateService();
        await service.ConfirmarPorCodigoAsync("482913");

        Assert.True(usuario.Ativo);
    }

    [Fact]
    public async Task ConfirmarPorTokenAsync_DeveLancarExcecao_QuandoExpirado()
    {
        var usuario = CriarUsuarioPendente();
        usuario.ConfirmacaoExpiraEm = DateTime.UtcNow.AddMinutes(-5);
        _usuarioRepository
            .Setup(r => r.ObterPorConfirmacaoTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();

        await Assert.ThrowsAsync<ConfirmacaoEmailInvalidaException>(() =>
            service.ConfirmarPorTokenAsync("token-plano-abc"));
    }

    [Fact]
    public async Task GerarEEnviarConfirmacaoAsync_DeveRegistrarEmailComTemplateHtml()
    {
        var usuario = new Usuario
        {
            Id = 1,
            Nome = "Gustavo",
            Email = "gustavo@email.com",
            Role = UserRole.Cliente,
            Ativo = false
        };

        RegistrarMensagemNotificacaoDto? mensagemRegistrada = null;
        _mensagemService
            .Setup(m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagemRegistrada = dto);

        var service = CreateService();

        var result = await service.GerarEEnviarConfirmacaoAsync(usuario);

        Assert.Equal("token-plano-abc", result.TokenPlano);
        Assert.Equal(6, result.CodigoPlano.Length);
        Assert.NotNull(mensagemRegistrada);
        Assert.Equal(CanalMensagemNotificacao.Email, mensagemRegistrada!.Canal);
        Assert.Equal("gustavo@email.com", mensagemRegistrada.Destinatario);
        Assert.Contains("<!doctype html>", mensagemRegistrada.Conteudo);
        Assert.Contains("class=\"confirm-code\"", mensagemRegistrada.Conteudo);
        Assert.Contains("Confirmar e-mail", mensagemRegistrada.Conteudo);
    }

    private static Usuario CriarUsuarioPendente() => new()
    {
        Id = 1,
        Email = "test@email.com",
        Nome = "Teste",
        Role = UserRole.Cliente,
        Ativo = false,
        ConfirmacaoTokenHash = "hash-token",
        ConfirmacaoCodigoHash = "hash-482913",
        ConfirmacaoExpiraEm = DateTime.UtcNow.AddHours(1)
    };

    private ConfirmacaoEmailService CreateService() => new(
        _usuarioRepository.Object,
        _tokenService.Object,
        _mensagemService.Object,
        Mock.Of<IAgendamentoConfirmacaoContaService>(),
        _confirmacaoWhatsAppService.Object,
        Options.Create(_authOptions),
        NullLogger<ConfirmacaoEmailService>.Instance);
}
