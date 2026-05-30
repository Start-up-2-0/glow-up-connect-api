using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ProfissionalServicoInvalidoException : DomainException
{
    public const string ErrorCode = "PROFISSIONAL_SERVICO_INVALIDO";

    public ProfissionalServicoInvalidoException(string mensagem)
        : base(mensagem, ErrorCode)
    {
    }
}
