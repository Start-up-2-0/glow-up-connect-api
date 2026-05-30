using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.DTOs.Servicos;
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
    private readonly IServicoNegocioService _servicoNegocioService;

    public AgendamentoPublicoController(
        IDisponibilidadeAgendaService disponibilidadeAgendaService,
        IServicoNegocioService servicoNegocioService)
    {
        _disponibilidadeAgendaService = disponibilidadeAgendaService;
        _servicoNegocioService = servicoNegocioService;
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

    [HttpGet("loja/{publicGuid:guid}/servicos")]
    public async Task<IActionResult> ListarServicosLoja(
        Guid publicGuid,
        CancellationToken cancellationToken)
    {
        var servicos = await _servicoNegocioService.ListarPublicosPorEstabelecimentoAsync(
            publicGuid,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ServicoPublicoResponseDto>>.From(
            "Servicos publicos da loja listados com sucesso.",
            servicos));
    }

    [HttpGet("profissional/{publicGuid:guid}/servicos")]
    public async Task<IActionResult> ListarServicosProfissionalAutonomo(
        Guid publicGuid,
        CancellationToken cancellationToken)
    {
        var servicos = await _servicoNegocioService.ListarPublicosPorProfissionalAutonomoAsync(
            publicGuid,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ServicoPublicoResponseDto>>.From(
            "Servicos publicos do profissional autonomo listados com sucesso.",
            servicos));
    }
}
