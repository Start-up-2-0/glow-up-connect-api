using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class TitularAssinaturaNaoEncontradoException : DomainException
{
    public const string ErrorCode = "TITULAR_ASSINATURA_NAO_ENCONTRADO";

    public TitularAssinaturaNaoEncontradoException()
        : base("Titular da assinatura nao encontrado ou indisponivel.", ErrorCode)
    {
    }
}
