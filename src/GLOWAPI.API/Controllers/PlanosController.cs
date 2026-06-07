using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Planos;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/planos")]
public class PlanosController : ControllerBase
{
    private readonly IPlanoService _planoService;

    public PlanosController(IPlanoService planoService)
    {
        _planoService = planoService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ListarAtivos(CancellationToken cancellationToken)
    {
        var resultado = await _planoService.ListarAtivosAsync(cancellationToken);
        return Ok(ApiSuccessResponse<PlanosAtivosResponseDto>.From(
            "Planos disponiveis para contratacao.",
            resultado));
    }
}
