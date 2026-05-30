using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class UsuarioSemVinculoNegocioException : DomainException
{
    public const string ErrorCode = "USUARIO_SEM_VINCULO_NEGOCIO";

    public UsuarioSemVinculoNegocioException()
        : base("Usuario nao possui vinculo ativo com este negocio.", ErrorCode)
    {
    }
}
