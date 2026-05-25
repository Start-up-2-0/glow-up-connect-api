using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Tests.Unit.Domain;

public class MensagemNotificacaoTests
{
    [Fact]
    public void PodeSerProcessada_DeveRetornarTrue_QuandoPendenteEAgendado()
    {
        var mensagem = CriarMensagem(StatusMensagemNotificacao.Pendente);
        mensagem.AgendadoPara = DateTime.UtcNow.AddMinutes(-1);

        Assert.True(mensagem.PodeSerProcessada(DateTime.UtcNow));
    }

    [Fact]
    public void PodeSerProcessada_DeveRetornarFalse_QuandoEnviado()
    {
        var mensagem = CriarMensagem(StatusMensagemNotificacao.Enviado);
        Assert.False(mensagem.PodeSerProcessada(DateTime.UtcNow));
    }

    [Fact]
    public void MarcarFalhaParaRetry_DeveMarcarFalhou_QuandoAtingirMaximoTentativas()
    {
        var mensagem = CriarMensagem(StatusMensagemNotificacao.Processando);
        mensagem.Tentativas = 4;
        mensagem.MaximoTentativas = 5;

        mensagem.MarcarFalhaParaRetry(DateTime.UtcNow, "erro", 30, 3600);

        Assert.Equal(StatusMensagemNotificacao.Falhou, mensagem.Status);
        Assert.Equal(5, mensagem.Tentativas);
        Assert.NotNull(mensagem.FalhouEm);
    }

    [Fact]
    public void MarcarFalhaParaRetry_DeveReprocessar_QuandoAindaHaTentativas()
    {
        var mensagem = CriarMensagem(StatusMensagemNotificacao.Processando);
        mensagem.Tentativas = 1;
        mensagem.MaximoTentativas = 5;

        mensagem.MarcarFalhaParaRetry(DateTime.UtcNow, "erro", 30, 3600);

        Assert.Equal(StatusMensagemNotificacao.Reprocessar, mensagem.Status);
        Assert.NotNull(mensagem.AgendadoPara);
    }

    [Fact]
    public void Cancelar_DeveLancar_QuandoJaEnviado()
    {
        var mensagem = CriarMensagem(StatusMensagemNotificacao.Enviado);
        Assert.Throws<InvalidOperationException>(() => mensagem.Cancelar(DateTime.UtcNow));
    }

    private static MensagemNotificacao CriarMensagem(StatusMensagemNotificacao status) => new()
    {
        Canal = CanalMensagemNotificacao.Email,
        Destinatario = "teste@email.com",
        Conteudo = "conteudo",
        Status = status,
        MaximoTentativas = 5
    };
}
