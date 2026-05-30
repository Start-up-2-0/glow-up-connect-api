using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ServicoNegocioInvalidoException : DomainException
{
    public const string ErrorCode = "SERVICO_NEGOCIO_INVALIDO";

    public ServicoNegocioInvalidoException(string mensagem)
        : base(mensagem, ErrorCode)
    {
    }
}
