using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class AvaliacaoConviteInvalidoException : DomainException
{
    public const string ErrorCode = "AVALIACAO_CONVITE_INVALIDO";

    public AvaliacaoConviteInvalidoException(string? mensagem = null)
        : base(mensagem ?? "Link de avaliacao invalido ou expirado.", ErrorCode)
    {
    }
}
