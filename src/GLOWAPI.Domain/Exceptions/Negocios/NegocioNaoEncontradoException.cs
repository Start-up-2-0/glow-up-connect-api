using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class NegocioNaoEncontradoException : DomainException
{
    public const string ErrorCode = "NEGOCIO_NAO_ENCONTRADO";

    public NegocioNaoEncontradoException()
        : base("Negocio nao encontrado.", ErrorCode)
    {
    }
}
