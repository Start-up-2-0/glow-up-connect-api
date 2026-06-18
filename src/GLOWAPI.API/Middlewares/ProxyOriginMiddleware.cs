using GLOWAPI.API.Models;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Middlewares;

public class ProxyOriginMiddleware
{
    private const string ForbiddenMessage = "Acesso negado.";
    private const string ForbiddenCode = "FORBIDDEN";

    private readonly RequestDelegate _next;
    private readonly ProxyOriginOptions _options;

    public ProxyOriginMiddleware(RequestDelegate next, IOptions<ProxyOriginOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled || DeveIgnorar(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ProxyOriginOptions.SecretHeaderName, out var provided)
            || !string.Equals(provided.ToString(), _options.Secret, StringComparison.Ordinal))
        {
            await WriteForbiddenAsync(context);
            return;
        }

        await _next(context);
    }

    private bool DeveIgnorar(PathString path)
    {
        var value = path.Value ?? string.Empty;
        if (value.Equals(_options.HealthPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return value.StartsWith(_options.WebhookPathPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteForbiddenAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiErrorResponse.From(ForbiddenMessage, ForbiddenCode));
    }
}
