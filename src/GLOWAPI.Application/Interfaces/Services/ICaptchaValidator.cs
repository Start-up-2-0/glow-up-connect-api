namespace GLOWAPI.Application.Interfaces.Services;

public interface ICaptchaValidator
{
    Task<bool> ValidarAsync(string captchaToken, string? remoteIp, CancellationToken cancellationToken = default);
}
