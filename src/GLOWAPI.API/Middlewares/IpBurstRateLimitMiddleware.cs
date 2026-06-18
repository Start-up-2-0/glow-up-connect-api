using GLOWAPI.API.Helpers;
using GLOWAPI.API.Models;
using GLOWAPI.Application.Interfaces.Services;

namespace GLOWAPI.API.Middlewares;

public class IpBurstRateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public IpBurstRateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IIpBurstRateLimitService rateLimitService, ISecurityAuditLogger auditLogger)
    {
        if (DeveIgnorar(context))
        {
            await _next(context);
            return;
        }

        var ip = ClientIpResolver.Resolver(context) ?? "127.0.0.1";

        var resultado = await rateLimitService.AvaliarAsync(ip, context.RequestAborted);
        if (resultado.Permitido)
        {
            await _next(context);
            return;
        }

        if (resultado.BloqueadoPorBurst)
        {
            await auditLogger.IpBurstBlockedAsync(ip, context.Request.Headers.UserAgent.ToString(), context.RequestAborted);
        }

        await WriteBlockedAsync(context, resultado.BlockedUntil);
    }

    private static bool DeveIgnorar(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            return true;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        return path.Equals("/health", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteBlockedAsync(HttpContext context, DateTime? blockedUntil = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.ContentType = "application/json";

        var message = blockedUntil.HasValue
            ? $"IP bloqueado por excesso de requisicoes ate {blockedUntil:O}."
            : "IP bloqueado por excesso de requisicoes.";

        await context.Response.WriteAsJsonAsync(ApiErrorResponse.From(message, "IP_BLOCKED_24H"));
    }
}
