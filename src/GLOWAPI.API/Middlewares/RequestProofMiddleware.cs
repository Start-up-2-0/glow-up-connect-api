using GLOWAPI.API.Helpers;
using GLOWAPI.API.Models;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Security;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;
using GLOWAPI.Infrastructure.Security; // Adicionado

namespace GLOWAPI.API.Middlewares;

public class RequestProofMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestProofOptions _options;
    private readonly ProxyOriginOptions _proxyOptions;

    public RequestProofMiddleware(
        RequestDelegate next,
        IOptions<RequestProofOptions> requestProofOptions,
        IOptions<ProxyOriginOptions> proxyOptions)
    {
        _next = next;
        _options = requestProofOptions.Value;
        _proxyOptions = proxyOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context, IRequestProofService requestProofService)
    {
        if (!_options.Enabled || DeveIgnorar(context))
        {
            await _next(context);
            return;
        }

        if (!PossuiProxySecretValido(context))
        {
            await _next(context);
            return;
        }

        var contextId = RequestProofContextCookieHelper.ObterContextId(context.Request, _options);
        var path = RequestProofPathNormalizer.NormalizePath(context.Request.Path.Value); // Alterado
        var method = context.Request.Method.ToUpperInvariant();

        context.Request.Headers.TryGetValue(_options.HeaderName, out var proofHeader);
        var resultado = requestProofService.ValidarEConsumir(
            proofHeader.ToString(),
            method,
            path,
            contextId);

        if (!resultado.Sucesso)
        {
            await WriteForbiddenAsync(context, resultado.Codigo);
            return;
        }

        await _next(context);
    }

    private bool PossuiProxySecretValido(HttpContext context) =>
        _proxyOptions.Enabled
        && context.Request.Headers.TryGetValue(ProxyOriginOptions.SecretHeaderName, out var provided)
        && string.Equals(provided.ToString(), _proxyOptions.Secret, StringComparison.Ordinal);

    private bool DeveIgnorar(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            return true;
        }

        var path = RequestProofPathNormalizer.NormalizePath(context.Request.Path.Value); // Alterado
        if (path.Equals(_proxyOptions.HealthPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.StartsWith(_proxyOptions.WebhookPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.Equals(_options.BootstrapPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static async Task WriteForbiddenAsync(HttpContext context, RequestProofFailureCode codigo)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var (message, code) = codigo switch
        {
            RequestProofFailureCode.Ausente => ("Request proof ausente.", "REQUEST_PROOF_AUSENTE"),
            RequestProofFailureCode.Expirado => ("Request proof expirado.", "REQUEST_PROOF_EXPIRADO"),
            RequestProofFailureCode.Replay => ("Request proof ja utilizado.", "REQUEST_PROOF_REPLAY"),
            RequestProofFailureCode.ContextoInvalido => ("Contexto de request proof invalido.", "REQUEST_PROOF_INVALIDO"),
            RequestProofFailureCode.MetodoPathInvalido => ("Request proof nao corresponde a rota.", "REQUEST_PROOF_INVALIDO"),
            _ => ("Request proof invalido.", "REQUEST_PROOF_INVALIDO")
        };

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiErrorResponse.From(message, code));
    }
}
