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
    private readonly AuthOptions _authOptions;

    public AuthController(
        IAuthService authService,
        IConfirmacaoEmailService confirmacaoEmailService,
        IConfirmacaoWhatsAppService confirmacaoWhatsAppService,
        IOptions<AuthOptions> authOptions)
    {
        _authService = authService;
        _confirmacaoEmailService = confirmacaoEmailService;
        _confirmacaoWhatsAppService = confirmacaoWhatsAppService;
        _authOptions = authOptions.Value;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, BuildSessionContext(), cancellationToken);
        return Ok(ApiSuccessResponse<LoginResponseDto>.From(
            "Login realizado com sucesso",
            LoginResponseDto.From(result)));
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
        return Ok(ApiSuccessResponse.From("Logout realizado com sucesso"));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request, BuildSessionContext(), cancellationToken);
        return Ok(ApiSuccessResponse<RefreshTokenResponseDto>.From(
            "Token renovado com sucesso",
            RefreshTokenResponseDto.From(result)));
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
    public async Task<IActionResult> ConfirmarWhatsApp(
        [FromBody] ConfirmarWhatsAppRequestDto request,
        CancellationToken cancellationToken)
    {
        var temToken = !string.IsNullOrWhiteSpace(request.Token);
        var temCodigo = !string.IsNullOrWhiteSpace(request.Codigo);

        if (temToken == temCodigo)
        {
            return BadRequest(ApiErrorResponse.From(
                "Informe exatamente token ou codigo.",
                "CONFIRMACAO_WHATSAPP_INVALIDA"));
        }

        if (temToken)
        {
            await _confirmacaoWhatsAppService.ConfirmarPorTokenAsync(request.Token!, cancellationToken);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Telefone))
            {
                return BadRequest(ApiErrorResponse.From(
                    "Telefone e obrigatorio para confirmacao por codigo.",
                    "CONFIRMACAO_WHATSAPP_INVALIDA"));
            }

            await _confirmacaoWhatsAppService.ConfirmarPorCodigoAsync(
                request.Telefone,
                request.Codigo!,
                cancellationToken);
        }

        return Ok(ApiSuccessResponse.From("WhatsApp confirmado com sucesso."));
    }

    [AllowAnonymous]
    [HttpPost("reenviar-confirmacao-whatsapp")]
    public async Task<IActionResult> ReenviarConfirmacaoWhatsApp(
        [FromBody] ReenviarConfirmacaoWhatsAppRequestDto request,
        CancellationToken cancellationToken)
    {
        await _confirmacaoWhatsAppService.ReenviarConfirmacaoAsync(request.Email, cancellationToken);
        return Ok(ApiSuccessResponse.From(
            "Se o e-mail estiver cadastrado e pendente de confirmacao WhatsApp, enviaremos um novo codigo."));
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
