using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/publico/agendar")]
[AllowAnonymous]
public class AgendamentoPublicoController : ControllerBase
{
    private readonly IDisponibilidadeAgendaService _disponibilidadeAgendaService;

    public AgendamentoPublicoController(IDisponibilidadeAgendaService disponibilidadeAgendaService)
    {
        _disponibilidadeAgendaService = disponibilidadeAgendaService;
    }

    [HttpGet("loja/{publicGuid:guid}/disponibilidade")]
    public async Task<IActionResult> ConsultarDisponibilidadeLoja(
        Guid publicGuid,
        [FromQuery] ConsultarDisponibilidadeAgendaDto request,
        CancellationToken cancellationToken)
    {
        var disponibilidade = await _disponibilidadeAgendaService.ConsultarPublicoPorEstabelecimentoAsync(
            publicGuid,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<DisponibilidadeAgendaResponseDto>.From(
            "Disponibilidade publica consultada com sucesso.",
            disponibilidade));
    }

    [HttpGet("profissional/{publicGuid:guid}/disponibilidade")]
    public async Task<IActionResult> ConsultarDisponibilidadeProfissionalAutonomo(
        Guid publicGuid,
        [FromQuery] ConsultarDisponibilidadeAgendaDto request,
        CancellationToken cancellationToken)
    {
        var disponibilidade = await _disponibilidadeAgendaService.ConsultarPublicoPorProfissionalAutonomoAsync(
            publicGuid,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<DisponibilidadeAgendaResponseDto>.From(
            "Disponibilidade publica do profissional autonomo consultada com sucesso.",
            disponibilidade));
    }
}
