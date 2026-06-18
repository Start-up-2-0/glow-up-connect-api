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
    private readonly ProxyOriginOptions _proxyOptions;
    private readonly MtlsOptions _mtlsOptions;

    public IpBurstRateLimitMiddleware(
        RequestDelegate next,
        IOptions<AuthOptions> authOptions,
        IOptions<ProxyOriginOptions> proxyOptions,
        IOptions<MtlsOptions> mtlsOptions)
    {
        _next = next;
        _authOptions = authOptions.Value;
        _proxyOptions = proxyOptions.Value;
        _mtlsOptions = mtlsOptions.Value;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IIpBurstRateLimitService rateLimitService,
        ISecurityAuditLogger auditLogger,
        IGlowTokenService glowTokenService)
    {
        if (DeveIgnorar(context))
        {
            await _next(context);
            return;
        }

        var ip = ClientIpResolver.Resolver(context) ?? "127.0.0.1";
        var path = context.Request.Path.Value ?? string.Empty;
        var trafegoConfiavel = EhTrafegoConfiavel(context, glowTokenService);

        var resultado = await rateLimitService.AvaliarAsync(ip, path, trafegoConfiavel, context.RequestAborted);
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

    private bool EhTrafegoConfiavel(HttpContext context, IGlowTokenService glowTokenService)
    {
        if (_proxyOptions.Enabled
            && context.Request.Headers.TryGetValue(ProxyOriginOptions.SecretHeaderName, out var proxySecret)
            && string.Equals(proxySecret.ToString(), _proxyOptions.Secret, StringComparison.Ordinal))
        {
            return true;
        }

        if (_mtlsOptions.Enabled && context.Connection.ClientCertificate is not null)
        {
            var thumbprints = _mtlsOptions.AllowedClientThumbprints
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Replace(":", string.Empty, StringComparison.OrdinalIgnoreCase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (thumbprints.Count == 0
                || thumbprints.Contains(context.Connection.ClientCertificate.Thumbprint))
            {
                return true;
            }
        }

        if (context.Request.Headers.TryGetValue(_authOptions.TokenHeaderName, out var tokenHeader)
            && !string.IsNullOrWhiteSpace(tokenHeader.ToString()))
        {
            var metadata = glowTokenService.ValidarMetadata(tokenHeader.ToString());
            if (metadata is not null && !glowTokenService.EstaExpirado(metadata, DateTime.UtcNow))
            {
                return true;
            }
        }

        return false;
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
