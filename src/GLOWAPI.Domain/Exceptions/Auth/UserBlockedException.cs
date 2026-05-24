namespace GLOWAPI.Domain.Exceptions.Auth;

public class UserBlockedException : AuthenticationException
{
    public const string ErrorCode = "USER_BLOCKED";

    public UserBlockedException()
        : base("Conta bloqueada após várias tentativas de login inválidas.", ErrorCode)
    {
    }
}
