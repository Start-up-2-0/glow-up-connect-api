using GLOWAPI.API.Attributes;
using GLOWAPI.API.Models;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.API.Middlewares;

/// <summary>
/// Middleware de permissoes por assinatura/modulo.
/// </summary>
public class PermissionMiddleware
{
    private readonly RequestDelegate _next;

    public PermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserContext currentUserContext,
        IModulosAssinaturaService modulosAssinaturaService)
    {
        var endpoint = context.GetEndpoint();
        var requisitos = endpoint?.Metadata.GetOrderedMetadata<RequerModuloAssinaturaAttribute>();
        if (requisitos is null || requisitos.Count == 0)
        {
            await _next(context);
            return;
        }

        if (!currentUserContext.IsAuthenticated)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Nao autorizado.",
                "UNAUTHORIZED");
            return;
        }

        foreach (var requisito in requisitos)
        {
            var titularId = ObterParametroId(context, requisito.ParametroId);
            if (!titularId.HasValue)
            {
                await WriteErrorAsync(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Identificador do titular da assinatura nao foi informado ou e invalido.",
                    "INVALID_SUBSCRIPTION_SCOPE");
                return;
            }

            var possuiModulo = requisito.TipoAssinatura switch
            {
                TipoAssinatura.Estabelecimento => await modulosAssinaturaService.PossuiModuloPorEstabelecimentoAsync(
                    titularId.Value,
                    requisito.Modulo,
                    context.RequestAborted),

                TipoAssinatura.ProfissionalAutonomo => await modulosAssinaturaService.PossuiModuloPorEstabelecimentoAsync(
                    titularId.Value,
                    requisito.Modulo,
                    context.RequestAborted),

                _ => false
            };

            if (!possuiModulo)
            {
                await WriteErrorAsync(
                    context,
                    StatusCodes.Status403Forbidden,
                    "Assinatura ativa com o modulo solicitado e obrigatoria para acessar este recurso.",
                    "SUBSCRIPTION_MODULE_BLOCKED");
                return;
            }
        }

        await _next(context);
    }

    private static int? ObterParametroId(HttpContext context, string parametroId)
    {
        if (context.Request.RouteValues.TryGetValue(parametroId, out var routeValue) &&
            int.TryParse(routeValue?.ToString(), out var routeId))
        {
            return routeId;
        }

        if (context.Request.Query.TryGetValue(parametroId, out var queryValue) &&
            int.TryParse(queryValue.FirstOrDefault(), out var queryId))
        {
            return queryId;
        }

        return null;
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
