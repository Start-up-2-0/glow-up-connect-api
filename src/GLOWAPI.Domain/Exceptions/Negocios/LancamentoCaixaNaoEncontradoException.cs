using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class LancamentoCaixaNaoEncontradoException : DomainException
{
    public const string ErrorCode = "LANCAMENTO_CAIXA_NAO_ENCONTRADO";

    public LancamentoCaixaNaoEncontradoException()
        : base("Lancamento de caixa nao encontrado.", ErrorCode)
    {
    }
}
