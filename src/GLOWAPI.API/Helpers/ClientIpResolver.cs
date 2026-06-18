namespace GLOWAPI.API.Helpers;

public static class ClientIpResolver
{
    public static string? Resolver(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor)
            && !string.IsNullOrWhiteSpace(forwardedFor.FirstOrDefault()))
        {
            var primeiroHop = forwardedFor.ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(primeiroHop))
            {
                return primeiroHop;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
