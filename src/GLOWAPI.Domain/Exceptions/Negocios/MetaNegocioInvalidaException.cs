using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class MetaNegocioInvalidaException : DomainException
{
    public const string ErrorCode = "META_NEGOCIO_INVALIDA";

    public MetaNegocioInvalidaException(string mensagem)
        : base(mensagem, ErrorCode)
    {
    }
}
