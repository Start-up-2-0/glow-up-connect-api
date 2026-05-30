using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class UltimoOwnerNegocioException : DomainException
{
    public const string ErrorCode = "ULTIMO_OWNER_NEGOCIO";

    public UltimoOwnerNegocioException()
        : base("O negocio deve manter pelo menos um owner ativo.", ErrorCode)
    {
    }
}
