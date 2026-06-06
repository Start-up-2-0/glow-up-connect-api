using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Auth;

public class ClienteSemAcessoNegocioException : DomainException
{
    public const string ErrorCode = "CLIENTE_SEM_ACESSO_NEGOCIO";

    public ClienteSemAcessoNegocioException()
        : base("Usuarios com role Cliente nao possuem acesso ao contexto de negocio.", ErrorCode)
    {
    }
}
