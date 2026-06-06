using GLOWAPI.API.Models;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Exceptions.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Middlewares;

public class GlowTokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthOptions _authOptions;

    public GlowTokenAuthenticationMiddleware(RequestDelegate next, IOptions<AuthOptions> authOptions)
    {
        _next = next;
        _authOptions = authOptions.Value;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuthSessionService authSessionService,
        ICurrentUserContext currentUserContext,
        ISecurityAuditLogger auditLogger)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null
            || EhRotaWebhookPublica(context))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(_authOptions.TokenHeaderName, out var tokenValues) ||
            string.IsNullOrWhiteSpace(tokenValues.FirstOrDefault()))
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "Não autorizado.", UnauthorizedException.ErrorCode);
            await auditLogger.AccessDeniedAsync("token_ausente", context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString());
            return;
        }

        var token = tokenValues.ToString();
        var sessionContext = new AuthSessionContext(
            context.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers.UserAgent.ToString());

        try
        {
            var auth = await authSessionService.ObterSessaoAtivaPorAccessTokenAsync(token, sessionContext, context.RequestAborted);

            currentUserContext.Set(
                auth.Usuario.Id,
                auth.Usuario.Email,
                auth.Usuario.Role,
                auth.Sessao.Id);

            var sessao = await authSessionService.ObterSessaoPorIdAsync(auth.Sessao.Id, context.RequestAborted);
            if (sessao is not null && DeveRenovarSessao(sessao.UltimaRenovacaoEm ?? sessao.LoginEm))
            {
                await authSessionService.RenovarExpiracaoAsync(sessao, context.RequestAborted);
            }

            await _next(context);
        }
        catch (AuthenticationException ex)
        {
            var statusCode = ex is UserBlockedException or InactiveUserException
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status401Unauthorized;

            await WriteErrorAsync(context, statusCode, ex.Message, ex.Code);
            await auditLogger.AccessDeniedAsync(ex.Code, sessionContext.Ip, sessionContext.UserAgent);
        }
    }

    private static bool EhRotaWebhookPublica(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return path.Equals("/api/webhooks/whatsapp/evolution/messages-upsert", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/webhooks/whatsapp/evolution/send-message", StringComparison.OrdinalIgnoreCase);
    }

    private bool DeveRenovarSessao(DateTime referencia)
    {
        var limite = referencia.AddMinutes(_authOptions.SlidingRenewalMinutes);
        return limite <= DateTime.UtcNow;
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message, string code)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiErrorResponse.From(message, code));
    }
}
