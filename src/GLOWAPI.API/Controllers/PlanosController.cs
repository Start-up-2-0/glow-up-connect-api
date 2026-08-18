using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Planos;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
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
    public async Task<IActionResult> ListarAtivos(
        [FromQuery] TipoAssinatura? tipoAssinatura,
        CancellationToken cancellationToken)
    {
        var tipo = tipoAssinatura ?? TipoAssinatura.Estabelecimento;
        var resultado = await _planoService.ListarAtivosAsync(tipo, cancellationToken);
        return Ok(ApiSuccessResponse<PlanosAtivosResponseDto>.From(
            "Planos disponiveis para contratacao.",
            resultado));
    }
}
