using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class MensagemNotificacao
{
    public int Id { get; set; }
    public Guid Guid { get; set; } = Guid.NewGuid();
    public int? EstabelecimentoId { get; set; }
    public CanalMensagemNotificacao Canal { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string Assunto { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public StatusMensagemNotificacao Status { get; set; } = StatusMensagemNotificacao.Pendente;
    public int Tentativas { get; set; }
    public int MaximoTentativas { get; set; } = 5;
    public int Prioridade { get; set; }
    public string? Provedor { get; set; }
    public DateTime? AgendadoPara { get; set; }
    public DateTime? ProcessamentoIniciadoEm { get; set; }
    public DateTime? EnviadoEm { get; set; }
    public DateTime? FalhouEm { get; set; }
    public string? MensagemErro { get; set; }
    public string? InstanciaWorker { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }

    public Estabelecimento? Estabelecimento { get; set; }
    public ICollection<MensagemNotificacaoLog> Logs { get; set; } = new List<MensagemNotificacaoLog>();

    public bool PodeSerProcessada(DateTime utcNow) =>
        Status is StatusMensagemNotificacao.Pendente or StatusMensagemNotificacao.Reprocessar
        && Tentativas < MaximoTentativas
        && (AgendadoPara is null || AgendadoPara <= utcNow);

    public bool PodeSerCancelada() =>
        Status is StatusMensagemNotificacao.Pendente or StatusMensagemNotificacao.Reprocessar;

    public void ReservarParaProcessamento(string instanciaWorker, DateTime utcNow)
    {
        if (!PodeSerProcessada(utcNow))
        {
            throw new InvalidOperationException("Mensagem nao pode ser reservada para processamento.");
        }

        Status = StatusMensagemNotificacao.Processando;
        InstanciaWorker = instanciaWorker;
        ProcessamentoIniciadoEm = utcNow;
        AtualizadoEm = utcNow;
    }

    public void MarcarEnviada(DateTime utcNow)
    {
        Status = StatusMensagemNotificacao.Enviado;
        EnviadoEm = utcNow;
        MensagemErro = null;
        InstanciaWorker = null;
        AtualizadoEm = utcNow;
    }

    public void MarcarFalhaParaRetry(DateTime utcNow, string erro, int backoffBaseSegundos, int backoffMaximoSegundos)
    {
        Tentativas++;
        MensagemErro = erro;
        InstanciaWorker = null;
        ProcessamentoIniciadoEm = null;
        AtualizadoEm = utcNow;

        if (Tentativas >= MaximoTentativas)
        {
            Status = StatusMensagemNotificacao.Falhou;
            FalhouEm = utcNow;
            AgendadoPara = null;
            return;
        }

        Status = StatusMensagemNotificacao.Reprocessar;
        var backoffSegundos = Math.Min(
            backoffBaseSegundos * (int)Math.Pow(2, Tentativas),
            backoffMaximoSegundos);
        AgendadoPara = utcNow.AddSeconds(backoffSegundos);
    }

    public void Cancelar(DateTime utcNow)
    {
        if (!PodeSerCancelada())
        {
            throw new InvalidOperationException("Mensagem nao pode ser cancelada.");
        }

        Status = StatusMensagemNotificacao.Cancelado;
        InstanciaWorker = null;
        ProcessamentoIniciadoEm = null;
        AtualizadoEm = utcNow;
    }

    public void RecuperarDeTravamento(DateTime utcNow)
    {
        if (Status != StatusMensagemNotificacao.Processando)
        {
            return;
        }

        Status = StatusMensagemNotificacao.Reprocessar;
        InstanciaWorker = null;
        ProcessamentoIniciadoEm = null;
        AtualizadoEm = utcNow;
    }
}
