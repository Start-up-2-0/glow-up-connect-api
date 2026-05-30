using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class HorarioIndisponivelException : DomainException
{
    public const string ErrorCode = "HORARIO_INDISPONIVEL";

    public HorarioIndisponivelException()
        : base("Horario indisponivel para agendamento.", ErrorCode)
    {
    }

    public HorarioIndisponivelException(string message)
        : base(message, ErrorCode)
    {
    }
}
