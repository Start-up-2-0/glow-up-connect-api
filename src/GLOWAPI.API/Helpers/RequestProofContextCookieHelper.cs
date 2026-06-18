using GLOWAPI.Application.Options;

namespace GLOWAPI.API.Helpers;

public static class RequestProofContextCookieHelper
{
    public static string ObterOuCriarContextId(
        HttpRequest request,
        HttpResponse response,
        RequestProofOptions options,
        IWebHostEnvironment environment)
    {
        if (request.Cookies.TryGetValue(options.ContextCookieName, out var existente)
            && !string.IsNullOrWhiteSpace(existente))
        {
            return existente;
        }

        var contextId = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        DefinirCookie(response, contextId, options, environment);
        return contextId;
    }

    public static void DefinirCookie(
        HttpResponse response,
        string contextId,
        RequestProofOptions options,
        IWebHostEnvironment environment)
    {
        response.Cookies.Append(options.ContextCookieName, contextId, new CookieOptions
        {
            HttpOnly = true,
            Secure = environment.IsProduction() || environment.IsStaging(),
            SameSite = SameSiteMode.Strict,
            Path = "/api",
            Expires = DateTimeOffset.UtcNow.AddHours(Math.Max(1, options.ContextCookieHours))
        });
    }

    public static string? ObterContextId(HttpRequest request, RequestProofOptions options)
    {
        if (request.Cookies.TryGetValue(options.ContextCookieName, out var contextId)
            && !string.IsNullOrWhiteSpace(contextId))
        {
            return contextId;
        }

        return null;
    }
}
