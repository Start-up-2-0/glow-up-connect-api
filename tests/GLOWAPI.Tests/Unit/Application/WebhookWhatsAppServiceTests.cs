using System.Text.Json;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Exceptions.Mensageria;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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
        var token = ConfirmacaoWhatsAppTokenHelper.Gerar(
            9, ConfirmacaoWhatsAppTokenHelper.TipoConta, "5579998755111");

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

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveLancar_QuandoApiKeyInvalidaEmStaging()
    {
        var service = CreateService(
            options: new MensageriaWhatsAppOptions
            {
                Habilitado = true,
                ApiKey = "chave-esperada",
                InstanceName = "glow-staging"
            },
            environmentName: "Staging");

        var payload = JsonDocument.Parse("""
            {
              "apikey": "chave-errada",
              "instance": "glow-staging",
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

        await Assert.ThrowsAsync<WebhookWhatsAppNaoAutorizadoException>(
            () => service.ProcessarMensagemRecebidaAsync(payload));
    }

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveProcessar_QuandoApiKeyValidaEmStaging()
    {
        var service = CreateService(
            options: new MensageriaWhatsAppOptions
            {
                Habilitado = true,
                ApiKey = "chave-esperada",
                InstanceName = "glow-staging"
            },
            environmentName: "Staging");

        var payload = JsonDocument.Parse("""
            {
              "apikey": "chave-esperada",
              "instance": "glow-staging",
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

        _inboundService.Verify(
            s => s.ProcessarAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<ConfirmacaoWhatsAppInboundContexto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveLancar_QuandoInstanciaInvalidaEmStaging()
    {
        var service = CreateService(
            options: new MensageriaWhatsAppOptions
            {
                Habilitado = true,
                ApiKey = "chave-esperada",
                InstanceName = "glow-staging"
            },
            environmentName: "Staging");

        var payload = JsonDocument.Parse("""
            {
              "apikey": "chave-esperada",
              "instance": "outra-instancia",
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

        await Assert.ThrowsAsync<WebhookWhatsAppNaoAutorizadoException>(
            () => service.ProcessarMensagemRecebidaAsync(payload));
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

    private WebhookWhatsAppService CreateService(
        MensageriaWhatsAppOptions? options = null,
        string environmentName = "Development")
    {
        return new WebhookWhatsAppService(
            _inboundService.Object,
            Options.Create(options ?? new MensageriaWhatsAppOptions { Habilitado = true }),
            new TestHostEnvironment(environmentName),
            NullLogger<WebhookWhatsAppService>.Instance);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "GLOWAPI.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
