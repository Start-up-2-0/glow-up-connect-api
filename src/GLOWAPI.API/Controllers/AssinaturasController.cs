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
}
