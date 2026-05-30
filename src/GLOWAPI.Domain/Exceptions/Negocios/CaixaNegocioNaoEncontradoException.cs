using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class CaixaNegocioNaoEncontradoException : DomainException
{
    public const string ErrorCode = "CAIXA_NEGOCIO_NAO_ENCONTRADO";

    public CaixaNegocioNaoEncontradoException()
        : base("Caixa do negocio nao encontrado.", ErrorCode)
    {
    }
}
