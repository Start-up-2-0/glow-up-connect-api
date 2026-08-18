using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class AvaliacaoNotaInvalidaException : DomainException
{
    public const string ErrorCode = "AVALIACAO_NOTA_INVALIDA";

    public AvaliacaoNotaInvalidaException()
        : base("A nota deve ser um valor inteiro entre 0 e 5.", ErrorCode)
    {
    }
}
