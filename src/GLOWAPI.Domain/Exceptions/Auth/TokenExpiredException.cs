namespace GLOWAPI.Domain.Exceptions.Auth;

public class TokenExpiredException : AuthenticationException
{
    public const string ErrorCode = "TOKEN_EXPIRED";

    public TokenExpiredException()
        : base("Token expirado.", ErrorCode)
    {
    }
}
