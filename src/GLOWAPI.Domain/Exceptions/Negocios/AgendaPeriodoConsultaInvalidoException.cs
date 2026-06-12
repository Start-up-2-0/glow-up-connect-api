using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class AgendaPeriodoConsultaInvalidoException : DomainException
{
    public const string ErrorCode = "AGENDA_PERIODO_CONSULTA_INVALIDO";

    public AgendaPeriodoConsultaInvalidoException(string mensagem)
        : base(mensagem, ErrorCode)
    {
    }
}
