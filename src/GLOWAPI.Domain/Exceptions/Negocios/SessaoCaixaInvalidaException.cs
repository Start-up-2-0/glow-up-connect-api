using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class SessaoCaixaInvalidaException : DomainException
{
    public const string ErrorCode = "SESSAO_CAIXA_INVALIDA";

    public SessaoCaixaInvalidaException(string mensagem)
        : base(mensagem, ErrorCode)
    {
    }
}
