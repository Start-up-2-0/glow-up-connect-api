using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class ProfissionalAutonomoAssinaturaInvalidoException : DomainException
{
    public const string ErrorCode = "PROFISSIONAL_AUTONOMO_ASSINATURA_INVALIDO";

    public ProfissionalAutonomoAssinaturaInvalidoException(string message)
        : base(message, ErrorCode)
    {
    }
}
