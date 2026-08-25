using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Security;

public class RecaptchaValidator : ICaptchaValidator
{
    private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

    /// <summary>
    /// Captcha desligado temporariamente em toda a plataforma.
    /// Para reativar: false aqui + Captcha__Enabled=true + SecretKey + VITE_CAPTCHA_SITE_KEY
    /// + CAPTCHA_TEMPORARILY_DISABLED=false nos useCaptcha (app/landing).
    /// </summary>
    private const bool TemporariamenteDesligado = true;

    private readonly HttpClient _httpClient;
    private readonly CaptchaOptions _options;
    private readonly ILogger<RecaptchaValidator> _logger;

    public RecaptchaValidator(
        HttpClient httpClient,
        IOptions<CaptchaOptions> options,
        ILogger<RecaptchaValidator> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> ValidarAsync(
        string captchaToken,
        string? remoteIp,
        CancellationToken cancellationToken = default)
    {
        if (TemporariamenteDesligado || !_options.Enabled)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(captchaToken))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            _logger.LogWarning("Captcha habilitado sem SecretKey configurada.");
            return false;
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = _options.SecretKey,
            ["response"] = captchaToken,
            ["remoteip"] = remoteIp ?? string.Empty
        });

        using var response = await _httpClient.PostAsync(VerifyUrl, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var payload = await response.Content.ReadFromJsonAsync<RecaptchaVerifyResponse>(cancellationToken);
        if (payload?.Success != true)
        {
            return false;
        }

        if (payload.Score.HasValue)
        {
            return payload.Score.Value >= _options.MinimumScore;
        }

        return true;
    }

    private sealed class RecaptchaVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public double? Score { get; set; }
    }
}
