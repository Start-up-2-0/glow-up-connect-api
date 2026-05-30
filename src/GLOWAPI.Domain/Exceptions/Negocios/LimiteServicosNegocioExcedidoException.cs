using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class LimiteServicosNegocioExcedidoException : DomainException
{
    public const string ErrorCode = "LIMITE_SERVICOS_NEGOCIO_EXCEDIDO";

    public LimiteServicosNegocioExcedidoException()
        : base("Limite de servicos ativos do plano contratado foi atingido para este negocio.", ErrorCode)
    {
    }
}
