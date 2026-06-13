using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class WebhookWhatsAppServiceTests
{
    private readonly Mock<IConfirmacaoWhatsAppInboundService> _inboundService = new();

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveDelegarParaInboundService_QuandoMensagemInboundValida()
    {
        var service = CreateService();
        var payload = CriarPayloadMensagem("5511988887777", "GLOW 482913", fromMe: false);

        await service.ProcessarMensagemRecebidaAsync(payload);

        _inboundService.Verify(
            s => s.ProcessarAsync(
                "5511988887777",
                "GLOW 482913",
                It.Is<ConfirmacaoWhatsAppInboundContexto>(ctx =>
                    ctx.RemoteJidConversa == "5511988887777@s.whatsapp.net"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveDelegarComContextoLid_QuandoPayloadUsaLid()
    {
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

        _inboundService.Verify(
            s => s.ProcessarAsync(
                "5511988887777",
                "GLOW 482913",
                It.Is<ConfirmacaoWhatsAppInboundContexto>(ctx =>
                    ctx.RemoteJidConversa == "69385314111689@lid"
                    && ctx.RemoteJidAlt == "5511988887777@s.whatsapp.net"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_NaoDeveDelegar_QuandoMensagemNaoEhConfirmacao()
    {
        var service = CreateService();
        var payload = CriarPayloadMensagem("5511988887777", "mande dnv o codigo", fromMe: true);

        await service.ProcessarMensagemRecebidaAsync(payload);

        _inboundService.Verify(
            s => s.ProcessarAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ConfirmacaoWhatsAppInboundContexto?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveDelegar_QuandoMensagemContemTokenBase64()
    {
        const string token = "NTU3OTk5ODc1NTExMQ==";

        var service = CreateService();
        var payload = JsonDocument.Parse($$"""
            {
              "data": {
                "key": {
                  "remoteJid": "60348602310753@lid",
                  "fromMe": false
                },
                "message": {
                  "conversation": "{{token}}"
                }
              }
            }
            """).RootElement;

        await service.ProcessarMensagemRecebidaAsync(payload);

        _inboundService.Verify(
            s => s.ProcessarAsync(
                string.Empty,
                token,
                It.Is<ConfirmacaoWhatsAppInboundContexto>(ctx =>
                    ctx.RemoteJidConversa == "60348602310753@lid"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarMensagemEnviadaAsync_NaoDeveDelegarConfirmacao()
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

        _inboundService.Verify(
            s => s.ProcessarAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ConfirmacaoWhatsAppInboundContexto?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static JsonElement CriarPayloadMensagem(string telefone, string texto, bool fromMe) =>
        JsonDocument.Parse($$"""
            {
              "event": "messages.upsert",
              "data": {
                "key": {
                  "remoteJid": "{{telefone}}@s.whatsapp.net",
                  "fromMe": {{fromMe.ToString().ToLowerInvariant()}}
                },
                "message": {
                  "conversation": "{{texto}}"
                }
              }
            }
            """).RootElement;

    private WebhookWhatsAppService CreateService() =>
        new(
            _inboundService.Object,
            Options.Create(new MensageriaWhatsAppOptions { Habilitado = true }),
            NullLogger<WebhookWhatsAppService>.Instance);
}
