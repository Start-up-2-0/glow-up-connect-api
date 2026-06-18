using GLOWAPI.API.Helpers;
using GLOWAPI.API.Models;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Middlewares;

public class IpBurstRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthOptions _authOptions;

    public IpBurstRateLimitMiddleware(RequestDelegate next, IOptions<AuthOptions> authOptions)
    {
        _next = next;
        _authOptions = authOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context, IIpBurstRateLimitService rateLimitService, ISecurityAuditLogger auditLogger)
    {
        if (DeveIgnorar(context))
        {
            await _next(context);
            return;
        }

        var ip = ClientIpResolver.Resolver(context) ?? "127.0.0.1";
        var path = context.Request.Path.Value ?? string.Empty;
        var possuiToken = context.Request.Headers.TryGetValue(_authOptions.TokenHeaderName, out var tokenHeader)
            && !string.IsNullOrWhiteSpace(tokenHeader.ToString());

        var resultado = await rateLimitService.AvaliarAsync(ip, path, possuiToken, context.RequestAborted);
        if (resultado.Permitido)
        {
            await _next(context);
            return;
        }

        if (resultado.BloqueadoPorBurst && resultado.ErrorCode == IpBurstRateLimitService.ErrorCodeHardBlock)
        {
            await auditLogger.IpBurstBlockedAsync(ip, context.Request.Headers.UserAgent.ToString(), context.RequestAborted);
        }

        await WriteBlockedAsync(context, resultado);
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

    private static async Task WriteBlockedAsync(HttpContext context, IpBurstRateLimitResult resultado)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.ContentType = "application/json";

        if (resultado.RetryAfterSeconds is > 0)
        {
            context.Response.Headers.RetryAfter = resultado.RetryAfterSeconds.Value.ToString();
        }

        var code = resultado.ErrorCode ?? IpBurstRateLimitService.ErrorCodeHardBlock;
        var message = code switch
        {
            IpBurstRateLimitService.ErrorCodeSoftBurst =>
                "Muitas requisicoes em pouco tempo. Aguarde alguns segundos e tente novamente.",
            _ when resultado.BlockedUntil.HasValue =>
                $"IP bloqueado por excesso de requisicoes ate {resultado.BlockedUntil:O}.",
            _ => "IP bloqueado por excesso de requisicoes."
        };

        await context.Response.WriteAsJsonAsync(ApiErrorResponse.From(message, code));
    }
}
