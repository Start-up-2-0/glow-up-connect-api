namespace GLOWAPI.Domain.Exceptions.Mensageria;

public class MensagemNotificacaoNaoCancelavelException : DomainException
{
    public const string ErrorCode = "MENSAGEM_NOTIFICACAO_NAO_CANCELAVEL";

    public MensagemNotificacaoNaoCancelavelException()
        : base("Mensagem de notificacao nao pode ser cancelada no status atual.", ErrorCode)
    {
    }
}
