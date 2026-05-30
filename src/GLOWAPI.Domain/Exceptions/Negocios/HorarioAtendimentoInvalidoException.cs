using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class HorarioAtendimentoInvalidoException : DomainException
{
    public const string ErrorCode = "HORARIO_ATENDIMENTO_INVALIDO";

    public HorarioAtendimentoInvalidoException(string mensagem)
        : base(mensagem, ErrorCode)
    {
    }
}
