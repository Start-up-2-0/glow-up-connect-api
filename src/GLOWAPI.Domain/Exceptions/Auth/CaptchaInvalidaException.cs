using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Auth;

public class CaptchaInvalidaException : DomainException
{
    public const string ErrorCode = "CAPTCHA_INVALIDO";

    public CaptchaInvalidaException(string message = "Captcha invalido ou expirado.")
        : base(message, ErrorCode)
    {
    }
}
