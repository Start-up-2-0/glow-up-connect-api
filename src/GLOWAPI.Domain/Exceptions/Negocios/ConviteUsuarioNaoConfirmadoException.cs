using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ConviteUsuarioNaoConfirmadoException : DomainException
{
    public const string ErrorCode = "CONVITE_USUARIO_NAO_CONFIRMADO";

    public ConviteUsuarioNaoConfirmadoException()
        : base("O usuario precisa confirmar o e-mail antes de ser vinculado.", ErrorCode)
    {
    }
}
