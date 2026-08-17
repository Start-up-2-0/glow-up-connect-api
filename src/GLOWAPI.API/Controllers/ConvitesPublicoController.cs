using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Convites;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/publico/convites")]
public class ConvitesPublicoController : ControllerBase
{
    private readonly IConviteNegocioService _conviteNegocioService;

    public ConvitesPublicoController(IConviteNegocioService conviteNegocioService)
    {
        _conviteNegocioService = conviteNegocioService;
    }

    [AllowAnonymous]
    [HttpGet("{token}/preview")]
    public async Task<IActionResult> ObterPreview(
        string token,
        CancellationToken cancellationToken)
    {
        var preview = await _conviteNegocioService.ObterPreviewAsync(token, cancellationToken);

        return Ok(ApiSuccessResponse<ConviteNegocioPreviewResponseDto>.From(
            "Convite encontrado.",
            preview));
    }

    [HttpPost("{token}/aceitar")]
    public async Task<IActionResult> Aceitar(
        string token,
        CancellationToken cancellationToken)
    {
        var convite = await _conviteNegocioService.AceitarAsync(token, cancellationToken);

        return Ok(ApiSuccessResponse<ConviteNegocioResponseDto>.From(
            "Convite aceito com sucesso.",
            convite));
    }

    [AllowAnonymous]
    [HttpPost("{token}/aceitar-com-cadastro")]
    public async Task<IActionResult> AceitarComCadastro(
        string token,
        [FromBody] AceitarConviteComCadastroRequestDto request,
        CancellationToken cancellationToken)
    {
        var convite = await _conviteNegocioService.AceitarComCadastroAsync(
            token,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<ConviteNegocioResponseDto>.From(
                "Conta criada e convite aceito com sucesso. Confirme seu e-mail para entrar.",
                convite));
    }
}
