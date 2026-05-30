using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.DTOs.Profissionais;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/profissionais-autonomos")]
public class ProfissionaisAutonomosController : ControllerBase
{
    private readonly IProfissionalAutonomoPerfilService _profissionalAutonomoPerfilService;
    private readonly IHorarioProfissionalAutonomoService _horarioProfissionalAutonomoService;

    public ProfissionaisAutonomosController(
        IProfissionalAutonomoPerfilService profissionalAutonomoPerfilService,
        IHorarioProfissionalAutonomoService horarioProfissionalAutonomoService)
    {
        _profissionalAutonomoPerfilService = profissionalAutonomoPerfilService;
        _horarioProfissionalAutonomoService = horarioProfissionalAutonomoService;
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

    [HttpGet("{profissionalId:int}/horarios")]
    public async Task<IActionResult> ListarHorarios(
        int profissionalId,
        [FromQuery] HorarioProfissionalAutonomoFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var horarios = await _horarioProfissionalAutonomoService.ListarAsync(
            profissionalId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<HorarioProfissionalResponseDto>>.From(
            "Horarios do profissional autonomo listados com sucesso.",
            horarios));
    }

    [HttpPost("{profissionalId:int}/horarios")]
    public async Task<IActionResult> CriarHorario(
        int profissionalId,
        [FromBody] CriarHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken)
    {
        var horario = await _horarioProfissionalAutonomoService.CriarAsync(
            profissionalId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<HorarioProfissionalResponseDto>.From(
                "Horario do profissional autonomo criado com sucesso.",
                horario));
    }

    [HttpPut("{profissionalId:int}/horarios/{horarioId:int}")]
    public async Task<IActionResult> AtualizarHorario(
        int profissionalId,
        int horarioId,
        [FromBody] AtualizarHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken)
    {
        var horario = await _horarioProfissionalAutonomoService.AtualizarAsync(
            profissionalId,
            horarioId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<HorarioProfissionalResponseDto>.From(
            "Horario do profissional autonomo atualizado com sucesso.",
            horario));
    }

    [HttpPatch("{profissionalId:int}/horarios/{horarioId:int}/status")]
    public async Task<IActionResult> AtualizarStatusHorario(
        int profissionalId,
        int horarioId,
        [FromBody] AtualizarStatusHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken)
    {
        var horario = await _horarioProfissionalAutonomoService.AtualizarStatusAsync(
            profissionalId,
            horarioId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<HorarioProfissionalResponseDto>.From(
            "Status do horario do profissional autonomo atualizado com sucesso.",
            horario));
    }
}
