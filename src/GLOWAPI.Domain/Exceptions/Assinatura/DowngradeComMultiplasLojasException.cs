using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class DowngradeComMultiplasLojasException : DomainException
{
    public const string ErrorCode = "DOWNGRADE_COM_MULTIPLAS_LOJAS";

    public DowngradeComMultiplasLojasException()
        : base(
            "Nao e possivel trocar para um plano de uma unidade enquanto houver mais de uma loja vinculada.",
            ErrorCode)
    {
    }
}
