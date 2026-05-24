namespace GLOWAPI.Domain.Exceptions.Auth;

public class InvalidCredentialsException : AuthenticationException
{
    public const string ErrorCode = "INVALID_CREDENTIALS";

    public InvalidCredentialsException()
        : base("Email ou senha inválidos", ErrorCode)
    {
    }
}
