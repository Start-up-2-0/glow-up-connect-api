using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class AssinaturaNotificacaoServiceTests
{
    private readonly Mock<IMensagemNotificacaoService> _mensagemNotificacaoService = new();

    [Fact]
    public async Task AssinaturaIniciadaAsync_DeveEnfileirarEmail()
    {
        RegistrarMensagemNotificacaoDto? mensagem = null;
        _mensagemNotificacaoService
            .Setup(s => s.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagem = dto)
            .ReturnsAsync(new MensagemNotificacaoResponseDto());

        var service = CreateService();

        await service.AssinaturaIniciadaAsync(
            new Assinatura
            {
                Id = 10,
                PlanoId = 2,
                EstabelecimentoId = 5,
                Status = AssinaturaStatus.PendentePagamento
            },
            new Plano { Id = 2, Nome = "Plano Pro" },
            " cliente@email.com ");

        Assert.NotNull(mensagem);
        Assert.Equal(CanalMensagemNotificacao.Email, mensagem!.Canal);
        Assert.Equal("cliente@email.com", mensagem.Destinatario);
        Assert.Equal("Assinatura iniciada", mensagem.Assunto);
        Assert.Contains("Plano Pro", mensagem.Conteudo);
        Assert.Equal(5, mensagem.EstabelecimentoId);
        Assert.Equal(2, mensagem.Prioridade);
        Assert.Contains("assinatura-iniciada", mensagem.PayloadJson);
        Assert.Contains("\"assinaturaId\":10", mensagem.PayloadJson);
    }

    [Fact]
    public async Task PagamentoConfirmadoAsync_DeveEnfileirarEmail()
    {
        RegistrarMensagemNotificacaoDto? mensagem = null;
        _mensagemNotificacaoService
            .Setup(s => s.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagem = dto)
            .ReturnsAsync(new MensagemNotificacaoResponseDto());

        var service = CreateService();

        await service.PagamentoConfirmadoAsync(
            new Assinatura { Id = 10, PlanoId = 2, Status = AssinaturaStatus.Ativa },
            new Pagamento { Id = 20, Valor = 99.90m },
            "cliente@email.com");

        Assert.NotNull(mensagem);
        Assert.Equal("Pagamento confirmado", mensagem!.Assunto);
        Assert.Contains("modulos", mensagem.Conteudo);
        Assert.Contains("pagamento-confirmado", mensagem.PayloadJson);
    }

    [Fact]
    public async Task AssinaturaSuspensaAsync_NaoDeveEnfileirar_QuandoDestinatarioNaoExiste()
    {
        var service = CreateService();

        await service.AssinaturaSuspensaAsync(
            new Assinatura { Id = 10, Status = AssinaturaStatus.Suspensa },
            " ");

        _mensagemNotificacaoService.Verify(s => s.RegistrarAsync(
            It.IsAny<RegistrarMensagemNotificacaoDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private AssinaturaNotificacaoService CreateService() =>
        new(_mensagemNotificacaoService.Object);
}
