using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class GatewayPagamentoException : DomainException
{
    public const string ErrorCode = "GATEWAY_PAGAMENTO_ERRO";

    public object? Details { get; }

    public GatewayPagamentoException(string message, object? details = null)
        : base(message, ErrorCode)
    {
        Details = details;
    }
}
