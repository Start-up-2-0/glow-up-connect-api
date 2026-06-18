using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Auth;

public class LoginIpRateLimitException : DomainException
{
    public const string ErrorCode = "LOGIN_IP_RATE_LIMITED";

    public LoginIpRateLimitException()
        : base("Muitas tentativas de login. Aguarde alguns minutos e tente novamente.", ErrorCode)
    {
    }
}
