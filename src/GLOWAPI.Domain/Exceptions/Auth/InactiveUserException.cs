namespace GLOWAPI.Domain.Exceptions.Auth;

public class InactiveUserException : AuthenticationException
{
    public const string ErrorCode = "USER_INACTIVE";

    public InactiveUserException()
        : base("Usuário inativo.", ErrorCode)
    {
    }
}
