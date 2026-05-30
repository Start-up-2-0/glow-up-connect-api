using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ProfissionalNegocioNaoEncontradoException : DomainException
{
    public const string ErrorCode = "PROFISSIONAL_NEGOCIO_NAO_ENCONTRADO";

    public ProfissionalNegocioNaoEncontradoException()
        : base("Profissional do negocio nao encontrado.", ErrorCode)
    {
    }
}
