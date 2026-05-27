using System.Net;
using GLOWAPI.API.Models;
using GLOWAPI.Domain.Exceptions;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Mensageria;
using GLOWAPI.Domain.Exceptions.Usuario;

namespace GLOWAPI.API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AuthenticationException ex)
        {
            var statusCode = ex switch
            {
                InvalidCredentialsException or UnauthorizedException or TokenExpiredException or InvalidTokenException
                    => HttpStatusCode.Unauthorized,
                UserBlockedException or InactiveUserException or EmailNaoConfirmadoException => HttpStatusCode.Forbidden,
                _ => HttpStatusCode.Unauthorized
            };

            await WriteErrorAsync(context, (int)statusCode, ex.Message, ex.Code);
            _logger.LogWarning(ex, "Falha de autenticação: {Code}", ex.Code);
        }
        catch (DomainException ex)
        {
            var statusCode = ex switch
            {
                EmailJaCadastradoException
                    or MensagemNotificacaoJaEnviadaException
                    or MensagemNotificacaoNaoCancelavelException
                    or AssinaturaDuplicadaException
                    => HttpStatusCode.Conflict,
                ConfirmacaoEmailInvalidaException
                    or AvatarInvalidoException
                    or AssinaturaTitularInvalidoException => HttpStatusCode.BadRequest,
                UsuarioSemPermissaoAssinaturaException => HttpStatusCode.Forbidden,
                MensagemNotificacaoNaoEncontradaException
                    or PlanoNaoEncontradoException
                    or TitularAssinaturaNaoEncontradoException => HttpStatusCode.NotFound,
                _ => HttpStatusCode.NotFound
            };

            await WriteErrorAsync(context, (int)statusCode, ex.Message, ex.Code);
            _logger.LogWarning(ex, "Regra de negócio violada: {Code}", ex.Code);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso não encontrado");
            await WriteLegacyErrorAsync(context, 404, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida");
            await WriteLegacyErrorAsync(context, 400, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno do servidor");
            await WriteLegacyErrorAsync(context, 500, "Erro interno do servidor");
        }
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

    private static async Task WriteLegacyErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
