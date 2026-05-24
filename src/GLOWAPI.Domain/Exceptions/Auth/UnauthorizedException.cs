namespace GLOWAPI.Domain.Exceptions.Auth;

public class UnauthorizedException : AuthenticationException
{
    public const string ErrorCode = "UNAUTHORIZED";

    public UnauthorizedException()
        : base("Não autorizado.", ErrorCode)
    {
    }

    public UnauthorizedException(string message)
        : base(message, ErrorCode)
    {
    }
}
