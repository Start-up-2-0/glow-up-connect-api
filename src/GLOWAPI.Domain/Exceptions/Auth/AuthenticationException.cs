namespace GLOWAPI.Domain.Exceptions.Auth;

public abstract class AuthenticationException : Exception
{
    protected AuthenticationException(string message, string code) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
