using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class RecebimentoAgendamentoInvalidoException : DomainException
{
    public const string ErrorCode = "RECEBIMENTO_AGENDAMENTO_INVALIDO";

    public RecebimentoAgendamentoInvalidoException(string mensagem)
        : base(mensagem, ErrorCode)
    {
    }
}
