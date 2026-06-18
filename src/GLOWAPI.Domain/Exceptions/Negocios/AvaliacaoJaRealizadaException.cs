using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class AvaliacaoJaRealizadaException : DomainException
{
    public const string ErrorCode = "AVALIACAO_JA_REALIZADA";

    public AvaliacaoJaRealizadaException()
        : base("Este agendamento ja foi avaliado.", ErrorCode)
    {
    }
}
