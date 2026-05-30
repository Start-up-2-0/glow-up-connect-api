using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class RecursoForaEscopoProfissionalException : DomainException
{
    public const string ErrorCode = "RECURSO_FORA_ESCOPO_PROFISSIONAL";

    public RecursoForaEscopoProfissionalException()
        : base("Recurso nao pertence ao escopo do profissional neste negocio.", ErrorCode)
    {
    }
}
