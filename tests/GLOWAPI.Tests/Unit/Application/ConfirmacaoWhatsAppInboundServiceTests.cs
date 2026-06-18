using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ConfirmacaoWhatsAppInboundServiceTests
{
    private readonly Mock<IConfirmacaoWhatsAppService> _confirmacaoWhatsAppService = new();
    private readonly Mock<IConfirmacaoWhatsAppEstabelecimentoService> _confirmacaoEstabelecimentoService = new();
    private readonly Mock<IConfirmacaoWhatsAppNotificacaoService> _notificacaoService = new();

    public ConfirmacaoWhatsAppInboundServiceTests()
    {
        ConfigurarResolverDestinoPadrao();
    }

    [Fact]
    public async Task ProcessarAsync_DeveConfirmarUsuario_EEnfileirarRespostas()
    {
        _confirmacaoWhatsAppService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.SucessoUsuario("Maria", "5511988887777", 1));

        var service = CreateService();

        await service.ProcessarAsync("5511988887777", "GLOW 482913", null);

        _notificacaoService.Verify(
            s => s.EnfileirarRespostaProcessandoAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<ConfirmacaoWhatsAppInboundContexto?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _notificacaoService.Verify(
            s => s.EnfileirarRespostaConfirmacaoSucessoAsync(
                It.Is<WhatsAppConfirmacaoInboundResultado>(r => r.Confirmado && r.UsuarioId == 1),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarAsync_DeveEnfileirarFalhaPorWhatsAppEEEmail_QuandoCodigoInvalido()
    {
        ConfigurarFalhaConfirmacaoUsuarioECodigoInvalido();

        var service = CreateService();

        await service.ProcessarAsync("5511988887777", "GLOW 482913", null);

        _notificacaoService.Verify(
            s => s.EnfileirarRespostaFalhaAsync(
                It.Is<WhatsAppConfirmacaoInboundResultado>(r =>
                    r.MotivoIgnorado == WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido),
                "5511988887777",
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarAsync_DeveEnfileirarJaConfirmado_SemEnfileirarFalha()
    {
        _confirmacaoWhatsAppService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado,
                "Maria",
                "5511988887777",
                "maria@email.com"));

        var service = CreateService();

        await service.ProcessarAsync("5511988887777", "GLOW 482913", null);

        _notificacaoService.Verify(
            s => s.EnfileirarRespostaJaConfirmadoAsync(
                It.Is<WhatsAppConfirmacaoInboundResultado>(r =>
                    r.MotivoIgnorado == WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _notificacaoService.Verify(
            s => s.EnfileirarRespostaFalhaAsync(
                It.IsAny<WhatsAppConfirmacaoInboundResultado>(),
                It.IsAny<string?>(),
                It.IsAny<ConfirmacaoWhatsAppInboundContexto?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessarAsync_NaoDeveProcessar_QuandoTextoNaoEhConfirmacao()
    {
        var service = CreateService();

        await service.ProcessarAsync("5511988887777", "mande dnv o codigo", null);

        _confirmacaoWhatsAppService.Verify(
            s => s.TentarConfirmarPorMensagemInboundAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _notificacaoService.Verify(
            s => s.EnfileirarRespostaProcessandoAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<ConfirmacaoWhatsAppInboundContexto?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void ConfigurarResolverDestinoPadrao()
    {
        _confirmacaoWhatsAppService
            .Setup(s => s.ResolverDestinoRespostaInboundAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string telefone, string _, CancellationToken _) =>
            {
                if (string.IsNullOrWhiteSpace(telefone))
                {
                    return null;
                }

                return new WhatsAppConfirmacaoInboundRespostaDestino(telefone, null);
            });

        _confirmacaoEstabelecimentoService
            .Setup(s => s.ResolverDestinoRespostaInboundAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string telefone, string _, CancellationToken _) =>
            {
                if (string.IsNullOrWhiteSpace(telefone))
                {
                    return null;
                }

                return new WhatsAppConfirmacaoInboundRespostaDestino(telefone, null);
            });
    }

    private void ConfigurarFalhaConfirmacaoUsuarioECodigoInvalido()
    {
        _confirmacaoWhatsAppService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido,
                "Maria",
                "5511988887777",
                "maria@email.com"));

        _confirmacaoEstabelecimentoService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.EntidadeNaoEncontrada));
    }

    private ConfirmacaoWhatsAppInboundService CreateService() =>
        new(
            _confirmacaoWhatsAppService.Object,
            _confirmacaoEstabelecimentoService.Object,
            _notificacaoService.Object,
            NullLogger<ConfirmacaoWhatsAppInboundService>.Instance);
}
