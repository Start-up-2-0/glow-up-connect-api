using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ProfissionalSemVinculoNegocioException : DomainException
{
    public const string ErrorCode = "PROFISSIONAL_SEM_VINCULO_NEGOCIO";

    public ProfissionalSemVinculoNegocioException()
        : base("Usuario nao possui vinculo profissional ativo com este negocio.", ErrorCode)
    {
    }
}
