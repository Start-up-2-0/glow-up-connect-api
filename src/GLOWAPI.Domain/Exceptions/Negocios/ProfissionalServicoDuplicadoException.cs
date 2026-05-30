using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ProfissionalServicoDuplicadoException : DomainException
{
    public const string ErrorCode = "PROFISSIONAL_SERVICO_DUPLICADO";

    public ProfissionalServicoDuplicadoException()
        : base("O profissional ja esta vinculado a este servico.", ErrorCode)
    {
    }
}
