using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class RecursoProfissionalNaoEncontradoException : DomainException
{
    public const string ErrorCode = "RECURSO_PROFISSIONAL_NAO_ENCONTRADO";

    public RecursoProfissionalNaoEncontradoException()
        : base("Recurso profissional nao encontrado.", ErrorCode)
    {
    }
}
