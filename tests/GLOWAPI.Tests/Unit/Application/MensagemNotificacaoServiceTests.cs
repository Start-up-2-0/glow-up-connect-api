using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Mensageria;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class MensagemNotificacaoServiceTests
{
    private readonly MensageriaOptions _options = new() { MaximoTentativasPadrao = 5 };

    [Fact]
    public async Task RegistrarAsync_DevePersistirMensagemPendente()
    {
        var repo = new Mock<IMensagemNotificacaoRepository>();
        var service = CreateService(repo.Object);

        var resultado = await service.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.Email,
            Destinatario = "user@email.com",
            Assunto = "Teste",
            Conteudo = "Corpo"
        });

        Assert.Equal(StatusMensagemNotificacao.Pendente, resultado.Status);
        repo.Verify(r => r.AdicionarAsync(It.IsAny<MensagemNotificacao>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarEnviadoAsync_DevePersistirMensagemEnviada()
    {
        var repo = new Mock<IMensagemNotificacaoRepository>();
        var service = CreateService(repo.Object);

        var resultado = await service.RegistrarEnviadoAsync(
            new RegistrarMensagemNotificacaoDto
            {
                Canal = CanalMensagemNotificacao.WhatsApp,
                Destinatario = "5579991917634",
                Assunto = "Confirmacao WhatsApp aprovada",
                Conteudo = "Confirmado"
            },
            "evolution-whatsapp-v1-textMessage");

        Assert.Equal(StatusMensagemNotificacao.Enviado, resultado.Status);
        repo.Verify(r => r.AdicionarAsync(
            It.Is<MensagemNotificacao>(m =>
                m.Status == StatusMensagemNotificacao.Enviado
                && m.Destinatario == "5579991917634"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarPorGuidAsync_DeveLancar_QuandoNaoEncontrada()
    {
        var repo = new Mock<IMensagemNotificacaoRepository>();
        repo.Setup(r => r.ObterPorGuidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MensagemNotificacao?)null);

        var service = CreateService(repo.Object);

        await Assert.ThrowsAsync<MensagemNotificacaoNaoEncontradaException>(() =>
            service.CancelarPorGuidAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CancelarPorGuidAsync_DeveLancar_QuandoJaEnviada()
    {
        var guid = Guid.NewGuid();
        var repo = new Mock<IMensagemNotificacaoRepository>();
        repo.Setup(r => r.ObterPorGuidAsync(guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MensagemNotificacao
            {
                Guid = guid,
                Status = StatusMensagemNotificacao.Enviado
            });

        var service = CreateService(repo.Object);

        await Assert.ThrowsAsync<MensagemNotificacaoJaEnviadaException>(() =>
            service.CancelarPorGuidAsync(guid));
    }

    private MensagemNotificacaoService CreateService(IMensagemNotificacaoRepository repository) =>
        new(repository, Options.Create(_options));
}
