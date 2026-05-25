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

public class ConfirmacaoEmailServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IGlowTokenService> _tokenService = new();
    private readonly Mock<IMensagemNotificacaoService> _mensagemService = new();
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
        Options.Create(_authOptions));
}
