using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class GatewayPagamentoException : DomainException
{
    public const string ErrorCode = "GATEWAY_PAGAMENTO_ERRO";

    public GatewayPagamentoException(string message)
        : base(message, ErrorCode)
    {
    }
}
