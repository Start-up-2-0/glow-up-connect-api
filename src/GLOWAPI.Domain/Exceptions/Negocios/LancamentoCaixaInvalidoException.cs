using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class LancamentoCaixaInvalidoException : DomainException
{
    public const string ErrorCode = "LANCAMENTO_CAIXA_INVALIDO";

    public LancamentoCaixaInvalidoException(string mensagem)
        : base(mensagem, ErrorCode)
    {
    }
}
