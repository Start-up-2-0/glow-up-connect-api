using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class AvaliacaoNaoElegivelException : DomainException
{
    public const string ErrorCode = "AVALIACAO_NAO_ELEGIVEL";

    public AvaliacaoNaoElegivelException(string? mensagem = null)
        : base(mensagem ?? "Este agendamento nao pode ser avaliado.", ErrorCode)
    {
    }
}
