using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Usuario;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ConfirmacaoWhatsAppServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IGlowTokenService> _tokenService = new();
    private readonly Mock<IMensagemNotificacaoService> _mensagemService = new();
    private readonly AuthOptions _authOptions = new()
    {
        ConfirmacaoWhatsAppHoras = 24,
        ConfirmacaoCodigoDigitos = 6
    };

    public ConfirmacaoWhatsAppServiceTests()
    {
        _tokenService.Setup(t => t.GerarRefreshToken()).Returns("token-plano-abc");
        _tokenService.Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns<string>(value => $"hash-{value}");
    }

    [Fact]
    public async Task ConfirmarPorCodigoAsync_DeveConfirmarWhatsApp_QuandoCodigoETelefoneValidos()
    {
        var usuario = CriarUsuarioPendenteWhatsApp();
        _usuarioRepository
            .Setup(r => r.ObterPorWhatsAppConfirmacaoCodigoHashAsync("hash-482913", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();
        await service.ConfirmarPorCodigoAsync("11988887777", "482913");

        Assert.NotNull(usuario.WhatsAppConfirmadoEm);
        Assert.True(usuario.WhatsAppOptIn);
        Assert.Null(usuario.WhatsAppConfirmacaoTokenHash);
        _usuarioRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmarPorTokenAsync_DeveLancarExcecao_QuandoExpirado()
    {
        var usuario = CriarUsuarioPendenteWhatsApp();
        usuario.WhatsAppConfirmacaoExpiraEm = DateTime.UtcNow.AddMinutes(-5);
        _usuarioRepository
            .Setup(r => r.ObterPorWhatsAppConfirmacaoTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();

        await Assert.ThrowsAsync<ConfirmacaoWhatsAppInvalidaException>(() =>
            service.ConfirmarPorTokenAsync("token-plano-abc"));
    }

    [Fact]
    public async Task IniciarConfirmacaoAsync_DeveEnviarEmailComLinkWaMe()
    {
        var usuario = new Usuario
        {
            Id = 1,
            Nome = "Maria",
            Email = "maria@email.com",
            Telefone = "11988887777",
            Role = UserRole.Cliente,
            Ativo = true
        };

        RegistrarMensagemNotificacaoDto? mensagemRegistrada = null;
        _mensagemService
            .Setup(m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagemRegistrada = dto);

        var service = CreateService();
        var instrucoes = await service.IniciarConfirmacaoAsync(usuario);

        Assert.Equal("5511999999999", instrucoes.NumeroPlataforma);
        Assert.StartsWith("GLOW ", instrucoes.MensagemSugerida);
        Assert.Contains("wa.me/5511999999999", instrucoes.LinkWhatsApp);
        Assert.True(instrucoes.EmailEnviado);
        Assert.NotNull(usuario.WhatsAppConfirmacaoCodigoHash);

        Assert.NotNull(mensagemRegistrada);
        Assert.Equal(CanalMensagemNotificacao.Email, mensagemRegistrada!.Canal);
        Assert.Equal("maria@email.com", mensagemRegistrada.Destinatario);
        Assert.Contains("wa.me/5511999999999", mensagemRegistrada.Conteudo);
    }

    [Fact]
    public async Task TentarConfirmarPorMensagemInboundAsync_DeveConfirmar_QuandoCodigoNaMensagem()
    {
        var usuario = CriarUsuarioPendenteWhatsApp();
        _usuarioRepository
            .Setup(r => r.ObterPorTelefoneNormalizadoAsync("5511988887777", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();
        var resultado = await service.TentarConfirmarPorMensagemInboundAsync(
            "5511988887777",
            "Ola! GLOW 482913");

        Assert.True(resultado.Confirmado);
        Assert.NotNull(usuario.WhatsAppConfirmadoEm);
    }

    [Fact]
    public async Task TentarConfirmarPorMensagemInboundAsync_DeveRetornarEntidadeNaoEncontrada_QuandoUsuarioInexistente()
    {
        _usuarioRepository
            .Setup(r => r.ObterPorTelefoneNormalizadoAsync("5511988887777", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var service = CreateService();
        var resultado = await service.TentarConfirmarPorMensagemInboundAsync(
            "5511988887777",
            "GLOW 482913");

        Assert.False(resultado.Confirmado);
        Assert.Equal(WhatsAppConfirmacaoInboundMotivoIgnorado.EntidadeNaoEncontrada, resultado.MotivoIgnorado);
    }

    [Fact]
    public async Task ResolverDestinoRespostaInboundAsync_DeveRetornarTelefoneCadastrado_QuandoCodigoIdentificaUsuario()
    {
        var usuario = CriarUsuarioPendenteWhatsApp();
        usuario.Telefone = "79998755111";
        _usuarioRepository
            .Setup(r => r.ObterPorWhatsAppConfirmacaoCodigoHashAsync("hash-482913", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();
        var destino = await service.ResolverDestinoRespostaInboundAsync(string.Empty, "GLOW 482913");

        Assert.NotNull(destino);
        Assert.Equal("5579998755111", destino!.Telefone);
        Assert.Equal("Maria", destino.Nome);
    }

    [Fact]
    public async Task TentarConfirmarPorMensagemInboundAsync_DeveConfirmarApenasPorCodigo_QuandoTelefoneAusente()
    {
        var usuario = CriarUsuarioPendenteWhatsApp();
        usuario.Telefone = "79998755111";
        _usuarioRepository
            .Setup(r => r.ObterPorWhatsAppConfirmacaoCodigoHashAsync("hash-482913", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();
        var resultado = await service.TentarConfirmarPorMensagemInboundAsync(
            string.Empty,
            "GLOW 482913");

        Assert.True(resultado.Confirmado);
        Assert.Equal("5579998755111", resultado.TelefoneResposta);
        Assert.NotNull(usuario.WhatsAppConfirmadoEm);
    }

    [Fact]
    public async Task TentarConfirmarPorMensagemInboundAsync_DeveRetornarCodigoInvalido_QuandoTelefoneAusenteECodigoInexistente()
    {
        _usuarioRepository
            .Setup(r => r.ObterPorWhatsAppConfirmacaoCodigoHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var service = CreateService();
        var resultado = await service.TentarConfirmarPorMensagemInboundAsync(
            string.Empty,
            "GLOW 000000");

        Assert.False(resultado.Confirmado);
        Assert.Equal(WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido, resultado.MotivoIgnorado);
    }

    [Fact]
    public async Task TentarConfirmarPorMensagemInboundAsync_DeveRetornarCodigoInvalido_QuandoMensagemSemCodigo()
    {
        var usuario = CriarUsuarioPendenteWhatsApp();
        _usuarioRepository
            .Setup(r => r.ObterPorTelefoneNormalizadoAsync("5511988887777", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();
        var resultado = await service.TentarConfirmarPorMensagemInboundAsync(
            "5511988887777",
            "Ola, tudo bem?");

        Assert.False(resultado.Confirmado);
        Assert.Equal(WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido, resultado.MotivoIgnorado);
    }

    [Fact]
    public async Task TentarConfirmarPorMensagemInboundAsync_DeveConfirmarPorCodigo_QuandoTelefoneWebhookEInstancia()
    {
        var usuario = CriarUsuarioPendenteWhatsApp();
        usuario.Telefone = "79998755111";
        _usuarioRepository
            .Setup(r => r.ObterPorWhatsAppConfirmacaoCodigoHashAsync("hash-482913", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _usuarioRepository
            .Setup(r => r.ObterPorTelefoneNormalizadoAsync("557991917634", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var service = CreateService();
        var resultado = await service.TentarConfirmarPorMensagemInboundAsync(
            "557991917634",
            "GLOW 482913");

        Assert.True(resultado.Confirmado);
        Assert.Equal("5579998755111", resultado.TelefoneResposta);
        Assert.NotNull(usuario.WhatsAppConfirmadoEm);
    }

    [Fact]
    public async Task ResolverDestinoRespostaInboundAsync_DevePreferirTelefoneCadastradoDoCodigo_SobreTelefoneWebhook()
    {
        var usuario = CriarUsuarioPendenteWhatsApp();
        usuario.Telefone = "79998755111";
        _usuarioRepository
            .Setup(r => r.ObterPorWhatsAppConfirmacaoCodigoHashAsync("hash-482913", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();
        var destino = await service.ResolverDestinoRespostaInboundAsync("557991917634", "GLOW 482913");

        Assert.NotNull(destino);
        Assert.Equal("5579998755111", destino!.Telefone);
        Assert.Equal("Maria", destino.Nome);
    }

    [Fact]
    public async Task ReenviarConfirmacaoAsync_DeveBuscarPorEmail()
    {
        var usuario = new Usuario
        {
            Id = 1,
            Nome = "Maria",
            Email = "maria@email.com",
            Telefone = "11988887777",
            Role = UserRole.Cliente,
            Ativo = true
        };

        _usuarioRepository
            .Setup(r => r.ObterPorEmailAsync("maria@email.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();
        await service.ReenviarConfirmacaoAsync("maria@email.com");

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto => dto.Destinatario == "maria@email.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private ConfirmacaoWhatsAppService CreateService() =>
        new(
            _usuarioRepository.Object,
            _tokenService.Object,
            _mensagemService.Object,
            Options.Create(new MensageriaWhatsAppOptions
            {
                NumeroPlataforma = "5511999999999"
            }),
            Options.Create(_authOptions),
            NullLogger<ConfirmacaoWhatsAppService>.Instance);

    private static Usuario CriarUsuarioPendenteWhatsApp() =>
        new()
        {
            Id = 1,
            Nome = "Maria",
            Email = "maria@email.com",
            Telefone = "11988887777",
            Role = UserRole.Cliente,
            Ativo = true,
            WhatsAppConfirmacaoTokenHash = "hash-token",
            WhatsAppConfirmacaoCodigoHash = "hash-482913",
            WhatsAppConfirmacaoExpiraEm = DateTime.UtcNow.AddHours(1)
        };
}
