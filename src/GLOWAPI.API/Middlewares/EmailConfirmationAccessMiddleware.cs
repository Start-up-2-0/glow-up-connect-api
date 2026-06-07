using GLOWAPI.API.Models;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Exceptions.Auth;
using Microsoft.AspNetCore.Authorization;

namespace GLOWAPI.API.Middlewares;

/// <summary>
/// Restringe usuarios com e-mail pendente de confirmacao ao conjunto minimo de rotas
/// necessarias para concluir onboarding (assinatura) e confirmar a conta.
/// </summary>
public class EmailConfirmationAccessMiddleware
{
    private static readonly HashSet<string> RotasPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/logout",
        "/api/usuario/me",
        "/api/usuario/me/estabelecimentos",
    };

    private readonly RequestDelegate _next;

    public EmailConfirmationAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserContext currentUserContext,
        IUsuarioRepository usuarioRepository)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null
            || !currentUserContext.IsAuthenticated
            || !currentUserContext.UserId.HasValue)
        {
            await _next(context);
            return;
        }

        var usuario = await usuarioRepository.ObterPorIdAsync(
            currentUserContext.UserId.Value,
            context.RequestAborted);

        if (usuario is null
            || usuario.Ativo
            || !usuario.PendenteConfirmacaoEmail()
            || PermiteAcesso(context))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiErrorResponse.From(
            "Confirme seu e-mail antes de acessar este recurso.",
            EmailNaoConfirmadoException.ErrorCode));
    }

    private static bool PermiteAcesso(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        if (method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase)
            && path.Equals("/api/assinaturas", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (method.Equals(HttpMethods.Get, StringComparison.OrdinalIgnoreCase)
            && path.Equals("/api/assinaturas/onboarding/contexto", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return RotasPermitidas.Contains(path);
    }
}
