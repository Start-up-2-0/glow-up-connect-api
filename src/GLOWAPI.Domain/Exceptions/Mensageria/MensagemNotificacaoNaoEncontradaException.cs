namespace GLOWAPI.Domain.Exceptions.Mensageria;

public class MensagemNotificacaoNaoEncontradaException : DomainException
{
    public const string ErrorCode = "MENSAGEM_NOTIFICACAO_NAO_ENCONTRADA";

    public MensagemNotificacaoNaoEncontradaException()
        : base("Mensagem de notificacao nao encontrada.", ErrorCode)
    {
    }
}
