using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ServicoNegocioNaoEncontradoException : DomainException
{
    public const string ErrorCode = "SERVICO_NEGOCIO_NAO_ENCONTRADO";

    public ServicoNegocioNaoEncontradoException()
        : base("Servico do negocio nao encontrado.", ErrorCode)
    {
    }
}
