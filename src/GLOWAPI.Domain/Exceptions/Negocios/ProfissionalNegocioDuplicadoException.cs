using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ProfissionalNegocioDuplicadoException : DomainException
{
    public const string ErrorCode = "PROFISSIONAL_NEGOCIO_DUPLICADO";

    public ProfissionalNegocioDuplicadoException()
        : base("Profissional ja possui vinculo ativo com este negocio.", ErrorCode)
    {
    }
}
