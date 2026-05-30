using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class LimiteUsuariosNegocioExcedidoException : DomainException
{
    public const string ErrorCode = "LIMITE_USUARIOS_NEGOCIO_EXCEDIDO";

    public LimiteUsuariosNegocioExcedidoException()
        : base("Limite de usuarios do plano contratado foi atingido para este negocio.", ErrorCode)
    {
    }
}
