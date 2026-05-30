using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ConviteNegocioInvalidoException : DomainException
{
    public const string ErrorCode = "CONVITE_NEGOCIO_INVALIDO";

    public ConviteNegocioInvalidoException(string mensagem)
        : base(mensagem, ErrorCode)
    {
    }
}
