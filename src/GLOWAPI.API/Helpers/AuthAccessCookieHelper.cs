using GLOWAPI.Application.Options;

namespace GLOWAPI.API.Helpers;

public static class AuthAccessCookieHelper
{
    public const string CookieName = "guc_access";

    public static void SetAccessCookie(
        HttpResponse response,
        string accessToken,
        DateTime accessExpiresAt,
        IHostEnvironment environment)
    {
        response.Cookies.Append(CookieName, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = environment.IsProduction() || environment.IsStaging(),
            SameSite = SameSiteMode.Strict,
            Path = "/api",
            Expires = accessExpiresAt.ToUniversalTime()
        });
    }

    public static void ClearAccessCookie(HttpResponse response, IHostEnvironment? environment = null)
    {
        response.Cookies.Delete(CookieName, new CookieOptions
        {
            Path = "/api",
            Secure = environment is null
                || environment.IsProduction()
                || environment.IsStaging(),
            SameSite = SameSiteMode.Strict
        });
    }

    /// <summary>
    /// Prefere o cookie HttpOnly; aceita header legado apenas como fallback (testes/clientes não-browser).
    /// </summary>
    public static string? ObterAccessToken(HttpRequest request, string? headerToken)
    {
        if (request.Cookies.TryGetValue(CookieName, out var cookieToken)
            && !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        return string.IsNullOrWhiteSpace(headerToken) ? null : headerToken;
    }

    public static string? ObterAccessToken(HttpRequest request, AuthOptions authOptions)
    {
        string? headerToken = null;
        if (request.Headers.TryGetValue(authOptions.TokenHeaderName, out var values))
        {
            headerToken = values.FirstOrDefault();
        }

        return ObterAccessToken(request, headerToken);
    }
}
