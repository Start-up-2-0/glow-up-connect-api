using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class AssinaturaDuplicadaException : DomainException
{
    public const string ErrorCode = "ASSINATURA_DUPLICADA";

    public AssinaturaDuplicadaException()
        : base("Ja existe uma assinatura ativa ou pendente para esta operacao.", ErrorCode)
    {
    }
}
