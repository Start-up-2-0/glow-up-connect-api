using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class HorarioFuncionamentoNaoEncontradoException : DomainException
{
    public const string ErrorCode = "HORARIO_FUNCIONAMENTO_NAO_ENCONTRADO";

    public HorarioFuncionamentoNaoEncontradoException()
        : base("Horario de funcionamento do estabelecimento nao encontrado.", ErrorCode)
    {
    }
}
