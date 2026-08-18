using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ComissaoProfissionalInvalidaException : DomainException
{
    public const string ErrorCode = "COMISSAO_PROFISSIONAL_INVALIDA";

    public ComissaoProfissionalInvalidaException(string mensagem)
        : base(mensagem, ErrorCode)
    {
    }
}
