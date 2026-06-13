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

    public WebhookWhatsAppServiceTests()
    {
        ConfigurarResolverDestinoPadrao();
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

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveConfirmarUsuario_QuandoMensagemRecebida()
    {
        _confirmacaoWhatsAppService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.SucessoUsuario("Maria", "5511988887777", 1));

        var service = CreateService();
        var payload = CriarPayloadMensagem("5511988887777", "GLOW 482913", fromMe: false);

        await service.ProcessarMensagemRecebidaAsync(payload);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.WhatsApp
                    && dto.Destinatario == "5511988887777"
                    && dto.Assunto == "Confirmacao WhatsApp em processamento"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.WhatsApp
                    && dto.Destinatario == "5511988887777"
                    && dto.Assunto == "Confirmacao WhatsApp aprovada"),
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
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.SucessoUsuario("Maria", "5511988887777", 1));

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
        ConfigurarFalhaConfirmacaoUsuarioECodigoInvalido();

        var service = CreateService();
        var payload = CriarPayloadMensagem("5511988887777", "GLOW 482913", fromMe: true);

        await service.ProcessarMensagemRecebidaAsync(payload);

        _confirmacaoWhatsAppService.Verify(
            s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveEnviarFalhaPorWhatsAppEEEmail_QuandoCodigoInvalido()
    {
        ConfigurarFalhaConfirmacaoUsuarioECodigoInvalido();

        var service = CreateService();
        var payload = CriarPayloadMensagem("5511988887777", "GLOW 482913", fromMe: false);

        await service.ProcessarMensagemRecebidaAsync(payload);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.WhatsApp
                    && dto.Assunto == "Confirmacao WhatsApp nao concluida"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.Email
                    && dto.Destinatario == "maria@email.com"
                    && dto.Assunto == "Nao conseguimos confirmar seu WhatsApp"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveInformarJaConfirmado_SemEnviarFalha()
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
        var payload = CriarPayloadMensagem("5511988887777", "GLOW 482913", fromMe: false);

        await service.ProcessarMensagemRecebidaAsync(payload);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Assunto == "WhatsApp ja confirmado"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Assunto == "Confirmacao WhatsApp nao concluida"),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveConfirmarUsuario_QuandoPayloadUsaLidComRemoteJidAlt()
    {
        _confirmacaoWhatsAppService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "GLOW 482913",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.SucessoUsuario("Maria", "5511988887777", 1));

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
    public async Task ProcessarMensagemRecebidaAsync_NaoDeveProcessar_MensagemSemGlow()
    {
        _confirmacaoWhatsAppService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                "5511988887777",
                "mande dnv o codigo",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado,
                "Maria",
                "5511988887777",
                "maria@email.com"));

        var service = CreateService();
        var payload = CriarPayloadMensagem("5511988887777", "mande dnv o codigo", fromMe: true);

        await service.ProcessarMensagemRecebidaAsync(payload);

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

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveProcessarLidSemTelefone_QuandoMensagemContemGlow()
    {
        _confirmacaoWhatsAppService
            .Setup(s => s.ResolverDestinoRespostaInboundAsync(
                string.Empty,
                "GLOW 691617",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WhatsAppConfirmacaoInboundRespostaDestino("5579998755111", "Thiago"));

        _confirmacaoWhatsAppService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                string.Empty,
                "GLOW 691617",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.SucessoUsuario("Thiago", "5579998755111", 13));

        var service = CreateService();
        var payload = JsonDocument.Parse("""
            {
              "data": {
                "key": {
                  "remoteJid": "60348602310753@lid",
                  "fromMe": false
                },
                "message": {
                  "extendedTextMessage": {
                    "text": "GLOW 691617"
                  }
                }
              },
              "sender": "557991917634@s.whatsapp.net"
            }
            """).RootElement;

        await service.ProcessarMensagemRecebidaAsync(payload);

        _confirmacaoWhatsAppService.Verify(
            s => s.TentarConfirmarPorMensagemInboundAsync(
                string.Empty,
                "GLOW 691617",
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Destinatario == "60348602310753@lid"
                    && dto.Assunto == "Confirmacao WhatsApp em processamento"
                    && dto.Conteudo.Contains("Thiago")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Destinatario == "60348602310753@lid"
                    && dto.Assunto == "Confirmacao WhatsApp aprovada"
                    && dto.Conteudo.Contains("Thiago")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarMensagemRecebidaAsync_DeveResponderNoLid_QuandoMensagemContemTokenBase64()
    {
        const string token = "NTU3OTk5ODc1NTExMQ==";

        _confirmacaoWhatsAppService
            .Setup(s => s.ResolverDestinoRespostaInboundAsync(
                string.Empty,
                token,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WhatsAppConfirmacaoInboundRespostaDestino("5579998755111", "Thiago"));

        _confirmacaoWhatsAppService
            .Setup(s => s.TentarConfirmarPorMensagemInboundAsync(
                string.Empty,
                token,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(WhatsAppConfirmacaoInboundResultado.SucessoUsuario("Thiago", "5579998755111", 13));

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

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Destinatario == "60348602310753@lid"
                    && dto.Assunto == "Confirmacao WhatsApp aprovada"
                    && dto.Conteudo.Contains("confirmado")),
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
            _confirmacaoWhatsAppService.Object,
            _confirmacaoEstabelecimentoService.Object,
            _mensagemService.Object,
            Options.Create(new MensageriaWhatsAppOptions()),
            NullLogger<WebhookWhatsAppService>.Instance);
}
