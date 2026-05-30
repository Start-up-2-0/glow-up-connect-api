using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class AgendamentoDadosClienteInvalidosException : DomainException
{
    public const string ErrorCode = "AGENDAMENTO_DADOS_CLIENTE_INVALIDOS";

    public AgendamentoDadosClienteInvalidosException()
        : base("Nome, e-mail e telefone do cliente sao obrigatorios para agendamento de visitante.", ErrorCode)
    {
    }

    public AgendamentoDadosClienteInvalidosException(string message)
        : base(message, ErrorCode)
    {
    }
}
