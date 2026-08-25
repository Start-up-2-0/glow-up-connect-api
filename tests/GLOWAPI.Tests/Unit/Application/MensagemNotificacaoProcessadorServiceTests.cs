using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class MensagemNotificacaoProcessadorServiceTests
{
    [Fact]
    public async Task ProcessarLoteAsync_DeveContinuar_QuandoUmaMensagemFalhaInesperadamente()
    {
        var mensagemOk = new MensagemNotificacao
        {
            Id = 1,
            Guid = Guid.NewGuid(),
            Canal = CanalMensagemNotificacao.Email,
            Destinatario = "ok@email.com",
            Conteudo = "ok",
            Status = StatusMensagemNotificacao.Processando,
            MaximoTentativas = 5
        };

        var mensagemErro = new MensagemNotificacao
        {
            Id = 2,
            Guid = Guid.NewGuid(),
            Canal = CanalMensagemNotificacao.Sms,
            Destinatario = "11999999999",
            Conteudo = "erro",
            Status = StatusMensagemNotificacao.Processando,
            MaximoTentativas = 5
        };

        var repo = new Mock<IMensagemNotificacaoRepository>();
        repo.Setup(r => r.ReservarLoteAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MensagemNotificacao> { mensagemOk, mensagemErro });

        var provedorOk = new Mock<IProvedorMensagem>();
        provedorOk.Setup(p => p.CanalSuportado).Returns(CanalMensagemNotificacao.Email);
        provedorOk.Setup(p => p.EnviarAsync(mensagemOk, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoEnvioMensagem(true, "{}", "{}", "ok", null, 10));

        var provedorSms = new Mock<IProvedorMensagem>();
        provedorSms.Setup(p => p.CanalSuportado).Returns(CanalMensagemNotificacao.Sms);
        provedorSms.Setup(p => p.EnviarAsync(mensagemErro, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha provedor"));

        var resolver = new ProvedorMensagemResolver(new[] { provedorOk.Object, provedorSms.Object });

        var service = new MensagemNotificacaoProcessadorService(
            repo.Object,
            resolver,
            Mock.Of<IUsuarioRepository>(),
            Mock.Of<IEstabelecimentoRepository>(),
            Options.Create(new MensageriaOptions { TamanhoLote = 10 }),
            NullLogger<MensagemNotificacaoProcessadorService>.Instance);

        var processadas = await service.ProcessarLoteAsync("worker-test");

        Assert.Equal(2, processadas);
        repo.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessarLoteAsync_DeveCancelarWhatsApp_QuandoConfirmacaoFoiRevogadaAposEnfileirar()
    {
        var mensagem = new MensagemNotificacao
        {
            Id = 3, Canal = CanalMensagemNotificacao.WhatsApp, Destinatario = "5579999999999",
            Conteudo = "alerta", Status = StatusMensagemNotificacao.Processando, UsuarioId = 7
        };
        var repo = new Mock<IMensagemNotificacaoRepository>();
        repo.Setup(r => r.ReservarLoteAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([mensagem]);
        var usuarios = new Mock<IUsuarioRepository>();
        usuarios.Setup(r => r.ObterPorIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario { Id = 7, Telefone = "5579999999999", WhatsAppConfirmadoEm = null });
        var provedor = new Mock<IProvedorMensagem>();
        provedor.Setup(p => p.CanalSuportado).Returns(CanalMensagemNotificacao.WhatsApp);

        var service = new MensagemNotificacaoProcessadorService(
            repo.Object, new ProvedorMensagemResolver([provedor.Object]), usuarios.Object,
            Mock.Of<IEstabelecimentoRepository>(), Options.Create(new MensageriaOptions { TamanhoLote = 10 }),
            NullLogger<MensagemNotificacaoProcessadorService>.Instance);

        await service.ProcessarLoteAsync("worker-test");

        Assert.Equal(StatusMensagemNotificacao.Cancelado, mensagem.Status);
        provedor.Verify(p => p.EnviarAsync(It.IsAny<MensagemNotificacao>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
