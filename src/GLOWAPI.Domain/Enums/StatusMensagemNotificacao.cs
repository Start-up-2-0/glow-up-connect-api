namespace GLOWAPI.Domain.Enums;

public enum StatusMensagemNotificacao
{
    Pendente = 1,
    Processando = 2,
    Enviado = 3,
    Falhou = 4,
    Reprocessar = 5,
    Cancelado = 6
}

// Extensao futura: Push, Webhook, NotificacaoInterna — adicionar em CanalMensagemNotificacao com nova migration.
