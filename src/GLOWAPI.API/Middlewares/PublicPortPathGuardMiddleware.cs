using GLOWAPI.API.Models;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Middlewares;

/// <summary>
/// Na porta HTTP publica (8080), permite apenas health e webhooks quando mTLS esta ativo.
/// </summary>
public class PublicPortPathGuardMiddleware
{
    private const string ForbiddenMessage = "Acesso negado.";
    private const string ForbiddenCode = "FORBIDDEN";

    private readonly RequestDelegate _next;
    private readonly MtlsOptions _options;
    private readonly ProxyOriginOptions _proxyOptions;

    public PublicPortPathGuardMiddleware(
        RequestDelegate next,
        IOptions<MtlsOptions> mtlsOptions,
        IOptions<ProxyOriginOptions> proxyOptions)
    {
        _next = next;
        _options = mtlsOptions.Value;
        _proxyOptions = proxyOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var localPort = context.Connection.LocalPort;
        if (localPort != _options.PublicPort)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.Equals(_proxyOptions.HealthPath, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(_proxyOptions.WebhookPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        await WriteForbiddenAsync(context);
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
