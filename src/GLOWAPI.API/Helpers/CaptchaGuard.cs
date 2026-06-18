using GLOWAPI.API.Helpers;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.API.Helpers;

public static class CaptchaGuard
{
    public static async Task GarantirValidoAsync(
        ICaptchaValidator captchaValidator,
        string? captchaToken,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var ip = ClientIpResolver.Resolver(context);
        if (await captchaValidator.ValidarAsync(captchaToken ?? string.Empty, ip, cancellationToken))
        {
            return;
        }

        throw new CaptchaInvalidaException();
    }
}
