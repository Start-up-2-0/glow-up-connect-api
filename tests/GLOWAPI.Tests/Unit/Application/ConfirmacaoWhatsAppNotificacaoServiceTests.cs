using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ConfirmacaoWhatsAppNotificacaoServiceTests
{
    private readonly Mock<IMensagemNotificacaoService> _mensagemService = new();

    [Fact]
    public async Task EnfileirarRespostaConfirmacaoSucessoAsync_DeveRegistrarEmailNaFila()
    {
        _mensagemService
            .Setup(m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MensagemNotificacaoResponseDto
            {
                Guid = Guid.NewGuid(),
                Canal = CanalMensagemNotificacao.Email,
                Status = StatusMensagemNotificacao.Pendente,
                CriadoEm = DateTime.UtcNow
            });

        var service = CreateService();
        var resultado = WhatsAppConfirmacaoInboundResultado.SucessoUsuario(
            "Maria",
            "5511988887777",
            1,
            "maria@email.com");

        await service.EnfileirarRespostaConfirmacaoSucessoAsync(resultado, null);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.Email
                    && dto.Destinatario == "maria@email.com"
                    &&                     dto.Assunto == "WhatsApp confirmado no Glow Up Connect"
                    && dto.Conteudo.Contains("Maria")
                    && dto.Conteudo.Contains("5511988887777")
                    && dto.Conteudo.Contains("<!doctype html>")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.WhatsApp),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnfileirarRespostaConfirmacaoSucessoAsync_NaoDeveRegistrar_QuandoEmailAusente()
    {
        var service = CreateService();
        var resultado = WhatsAppConfirmacaoInboundResultado.SucessoUsuario("Maria", "5511988887777", 1);

        await service.EnfileirarRespostaConfirmacaoSucessoAsync(resultado, null);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.IsAny<RegistrarMensagemNotificacaoDto>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnfileirarRespostaJaConfirmadoAsync_DeveRegistrarEmailNaFila()
    {
        _mensagemService
            .Setup(m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MensagemNotificacaoResponseDto
            {
                Guid = Guid.NewGuid(),
                Canal = CanalMensagemNotificacao.Email,
                Status = StatusMensagemNotificacao.Pendente,
                CriadoEm = DateTime.UtcNow
            });

        var service = CreateService();
        var resultado = WhatsAppConfirmacaoInboundResultado.Ignorado(
            WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado,
            "Maria",
            "5511988887777",
            "maria@email.com");

        await service.EnfileirarRespostaJaConfirmadoAsync(resultado, null);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.Email
                    && dto.Destinatario == "maria@email.com"
                    && dto.Assunto == "WhatsApp ja confirmado no Glow Up Connect"
                    && dto.Conteudo.Contains("Maria")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.WhatsApp),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnfileirarRespostaFalhaAsync_DeveRegistrarWhatsAppEEmailNaFila()
    {
        _mensagemService
            .Setup(m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MensagemNotificacaoResponseDto
            {
                Guid = Guid.NewGuid(),
                Canal = CanalMensagemNotificacao.WhatsApp,
                Status = StatusMensagemNotificacao.Pendente,
                CriadoEm = DateTime.UtcNow
            });

        var service = CreateService();
        var resultado = WhatsAppConfirmacaoInboundResultado.Ignorado(
            WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido,
            "Maria",
            "5511988887777",
            "maria@email.com");

        await service.EnfileirarRespostaFalhaAsync(resultado, null, null);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.WhatsApp),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mensagemService.Verify(
            m => m.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.Email
                    && dto.Destinatario == "maria@email.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private ConfirmacaoWhatsAppNotificacaoService CreateService() =>
        new(_mensagemService.Object, NullLogger<ConfirmacaoWhatsAppNotificacaoService>.Instance);
}
