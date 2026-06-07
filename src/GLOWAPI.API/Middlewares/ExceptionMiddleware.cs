using System.Net;
using GLOWAPI.API.Models;
using GLOWAPI.Domain.Exceptions;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Mensageria;
using GLOWAPI.Domain.Exceptions.Negocios;
using GLOWAPI.Domain.Exceptions.Pagamentos;
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
                    or EstabelecimentoOnboardingDuplicadoException
                    or UsuarioEquipeNegocioDuplicadoException
                    or ProfissionalNegocioDuplicadoException
                    or ProfissionalServicoDuplicadoException
                    or HorarioAtendimentoConflitanteException
                    or ConviteNegocioDuplicadoException
                    => HttpStatusCode.Conflict,
                GatewayPagamentoException => HttpStatusCode.BadGateway,
                ConfirmacaoEmailInvalidaException
                    or ConfirmacaoWhatsAppInvalidaException
                    or AvatarInvalidoException
                    or AssinaturaTitularInvalidoException
                    or CancelamentoAssinaturaInvalidoException
                    or EstabelecimentoAssinaturaInvalidoException
                    or PagamentoAssinaturaInvalidoException
                    or ProfissionalAutonomoAssinaturaInvalidoException
                    or TrocaPlanoAssinaturaInvalidaException
                    or WebhookPagamentoInvalidoException
                    or AtendimentoStatusInvalidoException
                    or ProfissionalServicoInvalidoException
                    or HorarioAtendimentoInvalidoException
                    or HorarioAlteracaoImpactaAgendamentosFuturosException
                    or UltimoOwnerNegocioException
                    or ConviteNegocioInvalidoException
                    or LimiteUsuariosNegocioExcedidoException
                    or LimiteProfissionaisNegocioExcedidoException
                    or LimiteServicosNegocioExcedidoException
                    or ServicoNegocioInvalidoException
                    or ProfissionalServicoComAgendamentoFuturoException
                    or AgendamentoDadosClienteInvalidosException
                    or AgendamentoStatusInvalidoException
                    or AgendamentoServicosInvalidosException
                    or EnderecoOperacaoInvalidoException
                    or LocalizacaoClienteInvalidaException => HttpStatusCode.BadRequest,
                HorarioIndisponivelException => HttpStatusCode.Conflict,
                UsuarioSemPermissaoAssinaturaException
                    or UsuarioSemPermissaoNegocioException
                    or UsuarioSemVinculoNegocioException
                    or ProfissionalSemVinculoNegocioException
                    or RecursoForaEscopoProfissionalException
                    or ClienteSemAcessoNegocioException => HttpStatusCode.Forbidden,
                MensagemNotificacaoNaoEncontradaException
                    or PlanoNaoEncontradoException
                    or AssinaturaNaoEncontradaException
                    or TitularAssinaturaNaoEncontradoException
                    or NegocioNaoEncontradoException
                    or UsuarioEquipeNegocioNaoEncontradoException
                    or RecursoProfissionalNaoEncontradoException
                    or CaixaNegocioNaoEncontradoException
                    or ServicoNegocioNaoEncontradoException
                    or HorarioAtendimentoNaoEncontradoException
                    or HorarioFuncionamentoNaoEncontradoException
                    or ProfissionalNegocioNaoEncontradoException
                    or ConviteNegocioNaoEncontradoException
                    or AgendamentoNaoEncontradoException => HttpStatusCode.NotFound,
                _ => HttpStatusCode.NotFound
            };

            var details = ex is HorarioAlteracaoImpactaAgendamentosFuturosException impacto
                ? impacto.AgendamentosImpactados
                : null;

            await WriteErrorAsync(context, (int)statusCode, ex.Message, ex.Code, details);
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
