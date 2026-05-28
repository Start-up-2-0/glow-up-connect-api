using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/estabelecimentos")]
public class EstabelecimentosController : ControllerBase
{
    private readonly IEstabelecimentoPerfilService _estabelecimentoPerfilService;

    public EstabelecimentosController(IEstabelecimentoPerfilService estabelecimentoPerfilService)
    {
        _estabelecimentoPerfilService = estabelecimentoPerfilService;
    }

    [HttpPut("{estabelecimentoId:int}/perfil")]
    public async Task<IActionResult> AtualizarPerfil(
        int estabelecimentoId,
        [FromBody] AtualizarEstabelecimentoPerfilDto request,
        CancellationToken cancellationToken)
    {
        var perfil = await _estabelecimentoPerfilService.AtualizarAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<EstabelecimentoPerfilResponseDto>.From(
            "Perfil do estabelecimento atualizado com sucesso.",
            perfil));
    }
}
