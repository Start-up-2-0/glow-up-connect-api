using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class AssinaturaTitularInvalidoException : DomainException
{
    public const string ErrorCode = "ASSINATURA_TITULAR_INVALIDO";

    public AssinaturaTitularInvalidoException()
        : base("Informe apenas o titular correspondente ao tipo da assinatura.", ErrorCode)
    {
    }
}
