namespace GLOWAPI.API.Helpers;

public static class AuthRefreshCookieHelper
{
    public const string CookieName = "guc_refresh";

    public static void SetRefreshCookie(
        HttpResponse response,
        string refreshToken,
        DateTime refreshExpiresAt,
        IHostEnvironment environment)
    {
        response.Cookies.Append(CookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = environment.IsProduction() || environment.IsStaging(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = refreshExpiresAt.ToUniversalTime()
        });
    }

    public static void ClearRefreshCookie(HttpResponse response, IHostEnvironment? environment = null)
    {
        response.Cookies.Delete(CookieName, new CookieOptions
        {
            Path = "/api/auth",
            Secure = environment is null
                || environment.IsProduction()
                || environment.IsStaging(),
            SameSite = SameSiteMode.Strict
        });
    }

    public static string? ObterRefreshToken(HttpRequest request, string? bodyRefreshToken)
    {
        if (request.Cookies.TryGetValue(CookieName, out var cookieToken)
            && !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        return string.IsNullOrWhiteSpace(bodyRefreshToken) ? null : bodyRefreshToken;
    }
}
