using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Profissionais;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/profissionais-autonomos")]
public class ProfissionaisAutonomosController : ControllerBase
{
    private readonly IProfissionalAutonomoPerfilService _profissionalAutonomoPerfilService;

    public ProfissionaisAutonomosController(IProfissionalAutonomoPerfilService profissionalAutonomoPerfilService)
    {
        _profissionalAutonomoPerfilService = profissionalAutonomoPerfilService;
    }

    [HttpPut("{profissionalId:int}/perfil")]
    public async Task<IActionResult> AtualizarPerfil(
        int profissionalId,
        [FromBody] AtualizarProfissionalAutonomoPerfilDto request,
        CancellationToken cancellationToken)
    {
        var perfil = await _profissionalAutonomoPerfilService.AtualizarAsync(
            profissionalId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ProfissionalAutonomoPerfilResponseDto>.From(
            "Perfil do profissional autonomo atualizado com sucesso.",
            perfil));
    }
}
