using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Helpers;
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
        FrontendBaseUrl = "http://localhost:3000",
        ConfirmacaoWhatsAppHoras = 24,
        ConfirmacaoCodigoDigitos = 6
    };

    private const string TokenConfirmacao = "NTUxMTk4ODg4Nzc3Nw==";

    public ConfirmacaoWhatsAppServiceTests()
    {
        _tokenService.Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns<string>(value => $"hash-{value}");
    }

    [Fact]
    public async Task ConfirmarPorCodigoAsync_DeveLancarExcecao_QuandoDescontinuado()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ConfirmacaoWhatsAppInvalidaException>(() =>
            service.ConfirmarPorCodigoAsync("11988887777", "482913"));
    }

    [Fact]
    public async Task IniciarConfirmacaoAsync_DeveEnviarWhatsAppEEmailComTokenBase64()
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

        var mensagens = new List<RegistrarMensagemNotificacaoDto>();
        _mensagemService
            .Setup(m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagens.Add(dto));

        var service = CreateService();
        var instrucoes = await service.IniciarConfirmacaoAsync(usuario);

        Assert.Equal("5511999999999", instrucoes.NumeroPlataforma);
        Assert.Equal(TokenConfirmacao, instrucoes.TokenConfirmacao);
        Assert.Contains("/c/NTUxMTk4ODg4Nzc3Nw==", instrucoes.LinkConfirmacao);
        Assert.Contains("wa.me/5511999999999", instrucoes.LinkWhatsApp);
        Assert.Contains(Uri.EscapeDataString(TokenConfirmacao), instrucoes.LinkWhatsApp);
        Assert.True(instrucoes.WhatsAppEnviado);
        Assert.True(instrucoes.EmailEnviado);
        Assert.Equal($"hash-{TokenConfirmacao}", usuario.WhatsAppConfirmacaoTokenHash);
        Assert.Null(usuario.WhatsAppConfirmacaoCodigoHash);

        Assert.Equal(2, mensagens.Count);
        Assert.Contains(mensagens, dto => dto.Canal == CanalMensagemNotificacao.WhatsApp);
        Assert.Contains(mensagens, dto => dto.Canal == CanalMensagemNotificacao.Email && dto.Destinatario == "maria@email.com");
    }

    [Fact]
    public async Task TentarConfirmarPorMensagemInboundAsync_DeveConfirmar_QuandoMensagemContemToken()
    {
        var usuario = CriarUsuarioPendenteWhatsApp();
        _usuarioRepository
            .Setup(r => r.ObterPorTelefoneNormalizadoAsync("5511988887777", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();
        var resultado = await service.TentarConfirmarPorMensagemInboundAsync(
            "5511988887777",
            TokenConfirmacao);

        Assert.True(resultado.Confirmado);
        Assert.NotNull(usuario.WhatsAppConfirmadoEm);
    }

    [Fact]
    public async Task TentarConfirmarPorMensagemInboundAsync_DeveConfirmarPorCodigoLegado_QuandoMensagemGlow()
    {
        var usuario = CriarUsuarioPendenteWhatsAppLegado();
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
            .Setup(r => r.ObterPorTelefoneNormalizadoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var service = CreateService();
        var resultado = await service.TentarConfirmarPorMensagemInboundAsync(
            "5511988887777",
            TokenConfirmacao);

        Assert.False(resultado.Confirmado);
        Assert.Equal(WhatsAppConfirmacaoInboundMotivoIgnorado.EntidadeNaoEncontrada, resultado.MotivoIgnorado);
    }

    [Fact]
    public async Task ResolverDestinoRespostaInboundAsync_DeveRetornarTelefoneCadastrado_QuandoTokenValido()
    {
        var usuario = CriarUsuarioPendenteWhatsApp();
        _usuarioRepository
            .Setup(r => r.ObterPorTelefoneNormalizadoAsync("5511988887777", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var service = CreateService();
        var destino = await service.ResolverDestinoRespostaInboundAsync("5511988887777", TokenConfirmacao);

        Assert.NotNull(destino);
        Assert.Equal("5511988887777", destino!.Telefone);
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
            m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
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
            WhatsAppConfirmacaoTokenHash = $"hash-{TokenConfirmacao}"
        };

    private static Usuario CriarUsuarioPendenteWhatsAppLegado() =>
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
