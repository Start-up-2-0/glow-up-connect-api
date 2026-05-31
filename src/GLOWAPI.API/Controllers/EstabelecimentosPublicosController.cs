using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/publico/estabelecimentos")]
[AllowAnonymous]
public class EstabelecimentosPublicosController : ControllerBase
{
    private readonly IEstabelecimentoDescobertaService _estabelecimentoDescobertaService;

    public EstabelecimentosPublicosController(IEstabelecimentoDescobertaService estabelecimentoDescobertaService)
    {
        _estabelecimentoDescobertaService = estabelecimentoDescobertaService;
    }

    [HttpGet("proximos")]
    public async Task<IActionResult> ListarProximos(
        [FromQuery] decimal latitude,
        [FromQuery] decimal longitude,
        [FromQuery] double? raioKm,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _estabelecimentoDescobertaService.ListarProximosAsync(
            latitude,
            longitude,
            raioKm,
            pagina,
            tamanhoPagina,
            cancellationToken);

        return Ok(ApiSuccessResponse<EstabelecimentosProximosPaginadoResponseDto>.From(
            "Estabelecimentos proximos listados com sucesso.",
            resultado));
    }

    [HttpGet("{publicGuid:guid}")]
    public async Task<IActionResult> ObterPorPublicGuid(
        Guid publicGuid,
        [FromQuery] decimal? latitude,
        [FromQuery] decimal? longitude,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _estabelecimentoDescobertaService.ObterPorPublicGuidAsync(
            publicGuid,
            latitude,
            longitude,
            cancellationToken);

        return Ok(ApiSuccessResponse<EstabelecimentoPublicoResponseDto>.From(
            "Estabelecimento obtido com sucesso.",
            resultado));
    }
}
