using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ProfissionalNegocioInvalidoException : DomainException
{
    public const string ErrorCode = "PROFISSIONAL_NEGOCIO_INVALIDO";

    public ProfissionalNegocioInvalidoException(string mensagem)
        : base(mensagem, ErrorCode)
    {
    }
}
