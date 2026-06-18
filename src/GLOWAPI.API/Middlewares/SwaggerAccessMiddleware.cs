using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Middlewares;

public class SwaggerAccessMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SwaggerOptions _options;

    public SwaggerAccessMiddleware(RequestDelegate next, IOptions<SwaggerOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.AccessKey))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Swagger-Key", out var provided)
            || !string.Equals(provided.ToString(), _options.AccessKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Swagger protegido. Informe o header X-Swagger-Key.");
            return;
        }

        await _next(context);
    }
}
