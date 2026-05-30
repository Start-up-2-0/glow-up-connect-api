using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class UsuarioSemPermissaoNegocioException : DomainException
{
    public const string ErrorCode = "USUARIO_SEM_PERMISSAO_NEGOCIO";

    public UsuarioSemPermissaoNegocioException()
        : base("Usuario nao possui permissao para acessar este recurso do negocio.", ErrorCode)
    {
    }
}
