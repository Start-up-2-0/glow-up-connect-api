using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class EstabelecimentoAssinaturaInvalidoException : DomainException
{
    public const string ErrorCode = "ESTABELECIMENTO_ASSINATURA_INVALIDO";

    public EstabelecimentoAssinaturaInvalidoException(string message)
        : base(message, ErrorCode)
    {
    }
}
