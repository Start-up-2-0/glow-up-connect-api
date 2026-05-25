namespace GLOWAPI.Domain.Exceptions.Mensageria;

public class MensagemNotificacaoJaEnviadaException : DomainException
{
    public const string ErrorCode = "MENSAGEM_NOTIFICACAO_JA_ENVIADA";

    public MensagemNotificacaoJaEnviadaException()
        : base("Mensagem de notificacao ja foi enviada.", ErrorCode)
    {
    }
}
