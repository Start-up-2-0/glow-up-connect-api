using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class AssinaturaNaoEncontradaException : DomainException
{
    public const string ErrorCode = "ASSINATURA_NAO_ENCONTRADA";

    public AssinaturaNaoEncontradaException()
        : base("Assinatura nao encontrada.", ErrorCode)
    {
    }
}
