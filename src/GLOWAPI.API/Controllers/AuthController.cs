using GLOWAPI.API.DTOs.Auth;
using GLOWAPI.API.Models;
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
    private readonly AuthOptions _authOptions;

    public AuthController(IAuthService authService, IOptions<AuthOptions> authOptions)
    {
        _authService = authService;
        _authOptions = authOptions.Value;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(
            request.Email,
            request.Senha,
            BuildSessionContext(),
            cancellationToken);

        var data = new LoginResponseDto
        {
            Token = result.Token,
            RefreshToken = result.RefreshToken,
            ExpiresAt = result.ExpiresAt,
            RefreshExpiresAt = result.RefreshExpiresAt,
            Usuario = new UsuarioAuthDto
            {
                Id = result.Usuario.Id,
                Nome = result.Usuario.Nome,
                Email = result.Usuario.Email,
                Role = result.Usuario.Role
            }
        };

        return Ok(ApiSuccessResponse<LoginResponseDto>.From("Login realizado com sucesso", data));
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
        var result = await _authService.RefreshAsync(
            request.RefreshToken,
            BuildSessionContext(),
            cancellationToken);

        return Ok(ApiSuccessResponse<object>.From("Token renovado com sucesso", new
        {
            token = result.Token,
            refreshToken = result.RefreshToken,
            expiresAt = result.ExpiresAt,
            refreshExpiresAt = result.RefreshExpiresAt
        }));
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
