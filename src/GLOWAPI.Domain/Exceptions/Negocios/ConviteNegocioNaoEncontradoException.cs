using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ConviteNegocioNaoEncontradoException : DomainException
{
    public const string ErrorCode = "CONVITE_NEGOCIO_NAO_ENCONTRADO";

    public ConviteNegocioNaoEncontradoException()
        : base("Convite do negocio nao encontrado.", ErrorCode)
    {
    }
}
