using System.Text.Json;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class WebhookWhatsAppServiceTests
{
    private readonly Mock<IConfirmacaoWhatsAppService> _confirmacaoWhatsAppService = new();
    private readonly Mock<IConfirmacaoWhatsAppEstabelecimentoService> _confirmacaoEstabelecimentoService = new();
    private readonly Mock<IMensagemNotificacaoService> _mensagemService = new();

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveConfirmarUsuario_QuandoMensagemRecebida()
    {
        _confirmacaoWhatsAppService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.SucessoUsuario("Maria", "5511988887777"));

        var service = CreateService();
        var payload = JsonDocument.Parse("""
            {
              "event": "messages.upsert",
              "data": {
                "key": {
                  "remoteJid": "5511988887777@s.whatsapp.net",
                  "fromMe": false
                },
                "message": {
                  "conversation": "GLOW 482913"
                }
              }
            }
            """).RootElement;

        await service.ProcessarMensagemRecebidaAsync(payload);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.WhatsApp
                    && dto.Destinatario == "5511988887777"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveConfirmarUsuario_SemCampoEventNoPayload()
    {
        _confirmacaoWhatsAppService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.SucessoUsuario("Maria", "5511988887777"));

        var service = CreateService();
        var payload = JsonDocument.Parse("""
            {
              "data": {
                "key": {
                  "remoteJid": "5511988887777@s.whatsapp.net",
                  "fromMe": false
                },
                "message": {
                  "conversation": "GLOW 482913"
                }
              }
            }
            """).RootElement;

        await service.ProcessarMensagemRecebidaAsync(payload);

        _confirmacaoWhatsAppService.Verify(
            s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveProcessar_MensagensFromMeTrue()
    {
        _confirmacaoWhatsAppService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido));

        _confirmacaoEstabelecimentoService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.EntidadeNaoEncontrada));

        var service = CreateService();
        var payload = JsonDocument.Parse("""
            {
              "event": "messages.upsert",
              "data": {
                "key": {
                  "remoteJid": "5511988887777@s.whatsapp.net",
                  "fromMe": true
                },
                "message": {
                  "conversation": "GLOW 482913"
                }
              }
            }
            """).RootElement;

        await service.ProcessarMensagemRecebidaAsync(payload);

        _confirmacaoWhatsAppService.Verify(
            s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveConfirmarUsuario_QuandoPayloadUsaLidComRemoteJidAlt()
    {
        _confirmacaoWhatsAppService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.SucessoUsuario("Maria", "5511988887777"));

        var service = CreateService();
        var payload = JsonDocument.Parse("""
            {
              "data": {
                "key": {
                  "remoteJid": "69385314111689@lid",
                  "remoteJidAlt": "5511988887777@s.whatsapp.net",
                  "fromMe": false
                },
                "message": {
                  "conversation": "GLOW 482913"
                }
              }
            }
            """).RootElement;

        await service.ProcessarMensagemRecebidaAsync(payload);

        _confirmacaoWhatsAppService.Verify(
            s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarMensagemEnviadaAsync_NaoDeveConfirmarUsuario()
    {
        var service = CreateService();
        var payload = JsonDocument.Parse("""
            {
              "event": "send.message",
              "data": {
                "key": {
                  "remoteJid": "5511988887777@s.whatsapp.net",
                  "fromMe": true
                },
                "message": {
                  "conversation": "Ola Maria"
                }
              }
            }
            """).RootElement;

        await service.ProcessarMensagemEnviadaAsync(payload);

        _confirmacaoWhatsAppService.Verify(
            s => s.TentarConfirmarPorMensagemInboundAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _mensagemService.Verify(
            m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private WebhookWhatsAppService CreateService() =>
        new(
            _confirmacaoWhatsAppService.Object,
            _confirmacaoEstabelecimentoService.Object,
            _mensagemService.Object,
            Options.Create(new MensageriaWhatsAppOptions()),
            NullLogger<WebhookWhatsAppService>.Instance);
}
