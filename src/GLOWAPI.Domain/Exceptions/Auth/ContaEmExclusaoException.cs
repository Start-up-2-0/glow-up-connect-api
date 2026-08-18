using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.Domain.Exceptions.Auth;

public class ContaEmExclusaoException : AuthenticationException
{
    public const string ErrorCode = "CONTA_EM_EXCLUSAO";

    public ContaEmExclusaoException(DateTime? reativarAte)
        : base(
            "Esta conta está em processo de exclusão. Você pode reativá-la até a data limite.",
            ErrorCode)
    {
        ReativarAte = reativarAte;
    }

    public DateTime? ReativarAte { get; }
}
