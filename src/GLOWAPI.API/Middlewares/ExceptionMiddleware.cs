using System.Net;
using GLOWAPI.API.Models;
using Microsoft.EntityFrameworkCore;
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
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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
                    or LimiteEstabelecimentosExcedidoException
                    or DowngradeComMultiplasLojasException
                    or UsuarioEquipeNegocioDuplicadoException
                    or ProfissionalNegocioDuplicadoException
                    or ProfissionalServicoDuplicadoException
                    or HorarioAtendimentoConflitanteException
                    or ConviteNegocioDuplicadoException
                    or AvaliacaoJaRealizadaException
                    => HttpStatusCode.Conflict,
                GatewayPagamentoException => HttpStatusCode.BadGateway,
                ConfirmacaoEmailInvalidaException
                    or ConfirmacaoWhatsAppInvalidaException
                    or CaptchaInvalidaException
                    or AvatarInvalidoException
                    or AssinaturaTitularInvalidoException
                    or CancelamentoAssinaturaInvalidoException
                    or EstabelecimentoAssinaturaInvalidoException
                    or PagamentoAssinaturaInvalidoException
                    or DiaVencimentoAssinaturaInvalidoException
                    or ProfissionalAutonomoAssinaturaInvalidoException
                    or TrocaPlanoAssinaturaInvalidaException
                    or WebhookPagamentoInvalidoException
                    or AtendimentoStatusInvalidoException
                    or ProfissionalServicoInvalidoException
                    or HorarioAtendimentoInvalidoException
                    or HorarioAlteracaoImpactaAgendamentosFuturosException
                    or UltimoOwnerNegocioException
                    or ConviteNegocioInvalidoException
                    or ConviteNegocioIndisponivelException
                    or ConviteUsuarioNaoConfirmadoException
                    or ProfissionalNegocioInvalidoException
                    or ProfissionalVitrineNegocioIndisponivelException
                    or LimiteUsuariosNegocioExcedidoException
                    or LimiteProfissionaisNegocioExcedidoException
                    or LimiteServicosNegocioExcedidoException
                    or ServicoNegocioInvalidoException
                    or ProfissionalServicoComAgendamentoFuturoException
                    or ProfissionalEquipeComAgendamentoFuturoException
                    or AgendamentoDadosClienteInvalidosException
                    or AgendamentoStatusInvalidoException
                    or AgendamentoServicosInvalidosException
                    or AgendaPeriodoConsultaInvalidoException
                    or EnderecoOperacaoInvalidoException
                    or LocalizacaoClienteInvalidaException
                    or AvaliacaoNaoElegivelException
                    or AvaliacaoNotaInvalidaException
                    or AvaliacaoConviteInvalidoException => HttpStatusCode.BadRequest,
                WebhookWhatsAppNaoAutorizadoException => HttpStatusCode.Unauthorized,
                LoginIpRateLimitException => HttpStatusCode.TooManyRequests,
                HorarioIndisponivelException => HttpStatusCode.Conflict,
                AgendamentoJaRecebidoException => HttpStatusCode.Conflict,
                LancamentoCaixaInvalidoException
                    or RecebimentoAgendamentoInvalidoException
                    or ComissaoProfissionalInvalidaException
                    or SessaoCaixaInvalidaException => HttpStatusCode.BadRequest,
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
                    or AgendamentoNaoEncontradoException
                    or LancamentoCaixaNaoEncontradoException => HttpStatusCode.NotFound,
                _ => HttpStatusCode.NotFound
            };

            var details = ex switch
            {
                HorarioAlteracaoImpactaAgendamentosFuturosException impacto => impacto.AgendamentosImpactados,
                ProfissionalEquipeComAgendamentoFuturoException profissionalEquipe => profissionalEquipe.AgendamentosFuturos,
                GatewayPagamentoException gateway => gateway.Details,
                _ => null
            };

            await WriteErrorAsync(context, (int)statusCode, ex.Message, ex.Code, details);
            _logger.LogWarning(ex, "Regra de negócio violada: {Code}", ex.Code);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso não encontrado");
            var message = _env.IsDevelopment() ? ex.Message : "Recurso não encontrado.";
            await WriteLegacyErrorAsync(context, 404, message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida");
            var message = _env.IsDevelopment() ? ex.Message : "Operação inválida.";
            await WriteLegacyErrorAsync(context, 400, message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Falha ao persistir no banco de dados");
            var mensagem = "Erro ao salvar dados. Por favor, tente novamente mais tarde."; // Mensagem generalizada
            await WriteErrorAsync(context, 500, mensagem, "DATABASE_UPDATE_ERROR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno do servidor: {ErrorMessage}", ex.Message);
            var message = _env.IsDevelopment() ? ex.Message : "Erro interno do servidor.";
            await WriteLegacyErrorAsync(context, 500, message);
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
