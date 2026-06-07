using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class DiaVencimentoAssinaturaInvalidoException : DomainException
{
    public const string ErrorCode = "DIA_VENCIMENTO_ASSINATURA_INVALIDO";

    public DiaVencimentoAssinaturaInvalidoException()
        : base("Dia de vencimento invalido. Valores permitidos: 5, 10, 15 ou 20.", ErrorCode)
    {
    }
}
