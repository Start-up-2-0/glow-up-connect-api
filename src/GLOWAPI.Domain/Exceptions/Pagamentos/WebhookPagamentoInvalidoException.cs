using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Pagamentos;

public class WebhookPagamentoInvalidoException : DomainException
{
    public const string ErrorCode = "WEBHOOK_PAGAMENTO_INVALIDO";

    public WebhookPagamentoInvalidoException(string message)
        : base(message, ErrorCode)
    {
    }
}
