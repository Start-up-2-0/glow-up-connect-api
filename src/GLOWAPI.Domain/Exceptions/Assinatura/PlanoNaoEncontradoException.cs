using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class PlanoNaoEncontradoException : DomainException
{
    public const string ErrorCode = "PLANO_NAO_ENCONTRADO";

    public PlanoNaoEncontradoException()
        : base("Plano nao encontrado ou indisponivel para contratacao.", ErrorCode)
    {
    }
}
