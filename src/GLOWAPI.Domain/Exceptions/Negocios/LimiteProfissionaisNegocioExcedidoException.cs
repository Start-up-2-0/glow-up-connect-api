using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class LimiteProfissionaisNegocioExcedidoException : DomainException
{
    public const string ErrorCode = "LIMITE_PROFISSIONAIS_NEGOCIO_EXCEDIDO";

    public LimiteProfissionaisNegocioExcedidoException()
        : base("Limite de profissionais do plano contratado foi atingido para este negocio.", ErrorCode)
    {
    }
}
