using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/publico/comodidades")]
[AllowAnonymous]
public class ComodidadesPublicasController : ControllerBase
{
    private readonly IComodidadePerfilService _comodidadePerfilService;

    public ComodidadesPublicasController(IComodidadePerfilService comodidadePerfilService)
    {
        _comodidadePerfilService = comodidadePerfilService;
    }

    [HttpGet]
    public async Task<IActionResult> ListarCatalogo(CancellationToken cancellationToken)
    {
        var comodidades = await _comodidadePerfilService.ListarCatalogoAsync(cancellationToken);
        return Ok(ApiSuccessResponse<IReadOnlyList<ComodidadeDto>>.From(
            "Comodidades disponiveis listadas com sucesso.",
            comodidades));
    }

    [HttpGet("estabelecimentos/{publicGuid:guid}")]
    public async Task<IActionResult> ListarPorEstabelecimento(Guid publicGuid, CancellationToken cancellationToken)
    {
        var comodidades = await _comodidadePerfilService.ListarPublicasAsync(publicGuid, cancellationToken);
        return Ok(ApiSuccessResponse<IReadOnlyList<ComodidadeDto>>.From(
            "Comodidades do estabelecimento listadas com sucesso.",
            comodidades));
    }
}
