using GLOWAPI.API.Attributes;
using GLOWAPI.API.Models;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions;
using GLOWAPI.Domain.Exceptions.Negocios;

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
        IModulosAssinaturaService modulosAssinaturaService,
        IAutorizacaoNegocioService autorizacaoNegocioService)
    {
        var endpoint = context.GetEndpoint();
        var requisitosModulo = endpoint?.Metadata.GetOrderedMetadata<RequerModuloAssinaturaAttribute>();
        var requisitosPermissao = endpoint?.Metadata.GetOrderedMetadata<RequerPermissaoNegocioAttribute>();
        if ((requisitosModulo is null || requisitosModulo.Count == 0)
            && (requisitosPermissao is null || requisitosPermissao.Count == 0))
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

        foreach (var requisito in requisitosModulo ?? Array.Empty<RequerModuloAssinaturaAttribute>())
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
                    "SUBSCRIPTION_MODULE_BLOCKED",
                    new { modulo = requisito.Modulo.ToString() });
                return;
            }
        }

        foreach (var requisito in requisitosPermissao ?? Array.Empty<RequerPermissaoNegocioAttribute>())
        {
            if (requisito.ParametroEhPublicGuid)
            {
                var publicGuid = ObterParametroGuid(context, requisito.ParametroId);
                if (!publicGuid.HasValue)
                {
                    await WriteErrorAsync(
                        context,
                        StatusCodes.Status400BadRequest,
                        "Identificador do negocio nao foi informado ou e invalido.",
                        "INVALID_BUSINESS_SCOPE");
                    return;
                }

                if (!await AutorizarPorPublicGuidAsync(
                        context,
                        autorizacaoNegocioService,
                        publicGuid.Value,
                        requisito.Permissao))
                {
                    return;
                }

                continue;
            }

            var estabelecimentoId = ObterParametroId(context, requisito.ParametroId);
            if (!estabelecimentoId.HasValue)
            {
                await WriteErrorAsync(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Identificador do negocio nao foi informado ou e invalido.",
                    "INVALID_BUSINESS_SCOPE");
                return;
            }

            if (!await AutorizarPorIdAsync(
                    context,
                    autorizacaoNegocioService,
                    estabelecimentoId.Value,
                    requisito.Permissao))
            {
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

    private static Guid? ObterParametroGuid(HttpContext context, string parametroId)
    {
        if (context.Request.RouteValues.TryGetValue(parametroId, out var routeValue) &&
            Guid.TryParse(routeValue?.ToString(), out var routeGuid))
        {
            return routeGuid;
        }

        if (context.Request.Query.TryGetValue(parametroId, out var queryValue) &&
            Guid.TryParse(queryValue.FirstOrDefault(), out var queryGuid))
        {
            return queryGuid;
        }

        return null;
    }

    private static async Task<bool> AutorizarPorIdAsync(
        HttpContext context,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        int estabelecimentoId,
        PermissaoNegocio permissao)
    {
        try
        {
            await autorizacaoNegocioService.AutorizarAsync(
                estabelecimentoId,
                permissao,
                context.RequestAborted);
            return true;
        }
        catch (DomainException ex) when (ex is UsuarioSemPermissaoNegocioException or UsuarioSemVinculoNegocioException)
        {
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, ex.Message, ex.Code);
            return false;
        }
        catch (NegocioNaoEncontradoException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message, ex.Code);
            return false;
        }
    }

    private static async Task<bool> AutorizarPorPublicGuidAsync(
        HttpContext context,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        Guid publicGuid,
        PermissaoNegocio permissao)
    {
        try
        {
            await autorizacaoNegocioService.AutorizarPorPublicGuidAsync(
                publicGuid,
                permissao,
                context.RequestAborted);
            return true;
        }
        catch (DomainException ex) when (ex is UsuarioSemPermissaoNegocioException or UsuarioSemVinculoNegocioException)
        {
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, ex.Message, ex.Code);
            return false;
        }
        catch (NegocioNaoEncontradoException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message, ex.Code);
            return false;
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string message,
        string code,
        object? details = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiErrorResponse.From(message, code, details));
    }
}
