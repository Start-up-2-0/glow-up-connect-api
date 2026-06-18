using GLOWAPI.API.Helpers;
using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Auth;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfirmacaoEmailService _confirmacaoEmailService;
    private readonly IConfirmacaoWhatsAppService _confirmacaoWhatsAppService;
    private readonly ICaptchaValidator _captchaValidator;
    private readonly AuthOptions _authOptions;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        IConfirmacaoEmailService confirmacaoEmailService,
        IConfirmacaoWhatsAppService confirmacaoWhatsAppService,
        ICaptchaValidator captchaValidator,
        IOptions<AuthOptions> authOptions,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _confirmacaoEmailService = confirmacaoEmailService;
        _confirmacaoWhatsAppService = confirmacaoWhatsAppService;
        _captchaValidator = captchaValidator;
        _authOptions = authOptions.Value;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        await CaptchaGuard.GarantirValidoAsync(_captchaValidator, request.CaptchaToken, HttpContext, cancellationToken);

        var result = await _authService.LoginAsync(request, BuildSessionContext(), cancellationToken);
        AuthRefreshCookieHelper.SetRefreshCookie(
            Response,
            result.RefreshToken,
            result.RefreshExpiresAt,
            _environment);

        var dto = LoginResponseDto.From(result);
        dto.RefreshToken = string.Empty;
        return Ok(ApiSuccessResponse<LoginResponseDto>.From(
            "Login realizado com sucesso",
            dto));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var token = ObterTokenDoHeader();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(ApiErrorResponse.From("Não autorizado.", "UNAUTHORIZED"));
        }

        await _authService.LogoutAsync(token, cancellationToken);
        AuthRefreshCookieHelper.ClearRefreshCookie(Response);
        return Ok(ApiSuccessResponse.From("Logout realizado com sucesso"));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var refreshToken = AuthRefreshCookieHelper.ObterRefreshToken(Request, request.RefreshToken);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(ApiErrorResponse.From("Refresh token ausente.", "INVALID_TOKEN"));
        }

        var result = await _authService.RefreshAsync(
            new RefreshTokenRequestDto { RefreshToken = refreshToken },
            BuildSessionContext(),
            cancellationToken);

        AuthRefreshCookieHelper.SetRefreshCookie(
            Response,
            result.RefreshToken,
            result.RefreshExpiresAt,
            _environment);

        var dto = RefreshTokenResponseDto.From(result);
        dto.RefreshToken = string.Empty;
        return Ok(ApiSuccessResponse<RefreshTokenResponseDto>.From(
            "Token renovado com sucesso",
            dto));
    }

    [AllowAnonymous]
    [HttpPost("confirmar-email")]
    public async Task<IActionResult> ConfirmarEmail([FromBody] ConfirmarEmailRequestDto request, CancellationToken cancellationToken)
    {
        var temToken = !string.IsNullOrWhiteSpace(request.Token);
        var temCodigo = !string.IsNullOrWhiteSpace(request.Codigo);

        if (temToken == temCodigo)
        {
            return BadRequest(ApiErrorResponse.From(
                "Informe exatamente token ou codigo.",
                "CONFIRMACAO_EMAIL_INVALIDA"));
        }

        if (temToken)
        {
            await _confirmacaoEmailService.ConfirmarPorTokenAsync(request.Token!, cancellationToken);
        }
        else
        {
            await _confirmacaoEmailService.ConfirmarPorCodigoAsync(request.Codigo!, cancellationToken);
        }

        return Ok(ApiSuccessResponse.From("E-mail confirmado com sucesso. Voce ja pode fazer login."));
    }

    [AllowAnonymous]
    [HttpPost("reenviar-confirmacao")]
    public async Task<IActionResult> ReenviarConfirmacao(
        [FromBody] ReenviarConfirmacaoRequestDto request,
        CancellationToken cancellationToken)
    {
        await _confirmacaoEmailService.ReenviarConfirmacaoAsync(request.Email, cancellationToken);
        return Ok(ApiSuccessResponse.From(
            "Se o e-mail estiver cadastrado e pendente de confirmacao, enviaremos um novo link e codigo."));
    }

    [AllowAnonymous]
    [HttpPost("confirmar-whatsapp")]
    public IActionResult ConfirmarWhatsApp() =>
        StatusCode(StatusCodes.Status410Gone, ApiErrorResponse.From(
            "Confirmacao manual por codigo foi descontinuada. Use o link enviado por WhatsApp ou e-mail.",
            "CONFIRMACAO_WHATSAPP_DESCONTINUADA"));

    [AllowAnonymous]
    [HttpPost("reenviar-confirmacao-whatsapp")]
    public async Task<IActionResult> ReenviarConfirmacaoWhatsApp(
        [FromBody] ReenviarConfirmacaoWhatsAppRequestDto request,
        CancellationToken cancellationToken)
    {
        await _confirmacaoWhatsAppService.ReenviarConfirmacaoAsync(request.Email, cancellationToken);
        return Ok(ApiSuccessResponse.From(
            "Se o e-mail estiver cadastrado e pendente de confirmacao WhatsApp, enviaremos novas instrucoes."));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword() =>
        StatusCode(StatusCodes.Status501NotImplemented,
            ApiErrorResponse.From("Recuperação de senha ainda não implementada.", "NOT_IMPLEMENTED"));

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public IActionResult ResetPassword() =>
        StatusCode(StatusCodes.Status501NotImplemented,
            ApiErrorResponse.From("Redefinição de senha ainda não implementada.", "NOT_IMPLEMENTED"));

    private AuthSessionContext BuildSessionContext() =>
        new(HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());

    private string? ObterTokenDoHeader()
    {
        if (Request.Headers.TryGetValue(_authOptions.TokenHeaderName, out var values))
        {
            return values.FirstOrDefault();
        }

        return null;
    }
}
