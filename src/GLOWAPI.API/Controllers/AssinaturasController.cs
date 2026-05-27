using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/assinaturas")]
public class AssinaturasController : ControllerBase
{
    private readonly IAssinaturaService _assinaturaService;

    public AssinaturasController(IAssinaturaService assinaturaService)
    {
        _assinaturaService = assinaturaService;
    }

    [HttpPost]
    public async Task<IActionResult> Iniciar(
        [FromBody] IniciarAssinaturaRequestDto request,
        CancellationToken cancellationToken)
    {
        var assinatura = await _assinaturaService.IniciarAsync(request, cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<AssinaturaResponseDto>.From("Assinatura iniciada com sucesso.", assinatura));
    }

    [HttpPost("{assinaturaId:int}/trocar-plano")]
    public async Task<IActionResult> TrocarPlano(
        int assinaturaId,
        [FromBody] TrocarPlanoAssinaturaRequestDto request,
        CancellationToken cancellationToken)
    {
        var assinatura = await _assinaturaService.TrocarPlanoAsync(assinaturaId, request, cancellationToken);
        return Ok(ApiSuccessResponse<AssinaturaResponseDto>.From(
            "Solicitacao de troca de plano registrada com sucesso.",
            assinatura));
    }

    [HttpPost("{assinaturaId:int}/cancelar")]
    public async Task<IActionResult> Cancelar(
        int assinaturaId,
        CancellationToken cancellationToken)
    {
        var assinatura = await _assinaturaService.CancelarAsync(assinaturaId, cancellationToken);
        return Ok(ApiSuccessResponse<AssinaturaResponseDto>.From(
            "Assinatura cancelada com sucesso.",
            assinatura));
    }
}
