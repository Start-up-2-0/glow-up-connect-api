namespace GLOWAPI.Domain.Exceptions.Auth;

public class InvalidTokenException : AuthenticationException
{
    public const string ErrorCode = "INVALID_TOKEN";

    public InvalidTokenException()
        : base("Token inválido.", ErrorCode)
    {
    }
}
