using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Agendamento;
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
    private readonly IAgendamentoNegocioService _agendamentoNegocioService;

    public AgendamentoPublicoController(
        IDisponibilidadeAgendaService disponibilidadeAgendaService,
        IServicoNegocioService servicoNegocioService,
        IAgendamentoNegocioService agendamentoNegocioService)
    {
        _disponibilidadeAgendaService = disponibilidadeAgendaService;
        _servicoNegocioService = servicoNegocioService;
        _agendamentoNegocioService = agendamentoNegocioService;
    }

    [HttpGet("loja/{publicGuid:guid}/profissionais")]
    public async Task<IActionResult> ListarProfissionaisLoja(
        Guid publicGuid,
        CancellationToken cancellationToken)
    {
        var profissionais = await _agendamentoNegocioService.ListarProfissionaisPublicosPorLojaAsync(
            publicGuid,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ProfissionalPublicoResponseDto>>.From(
            "Profissionais publicos da loja listados com sucesso.",
            profissionais));
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
    public async Task<IActionResult> ConsultarDisponibilidadeProfissional(
        Guid publicGuid,
        [FromQuery] ConsultarDisponibilidadeAgendaDto request,
        CancellationToken cancellationToken)
    {
        var disponibilidade = await _disponibilidadeAgendaService.ConsultarPublicoPorProfissionalAsync(
            publicGuid,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<DisponibilidadeAgendaResponseDto>.From(
            "Disponibilidade publica do profissional consultada com sucesso.",
            disponibilidade));
    }

    [HttpGet("loja/{publicGuid:guid}/servicos")]
    public async Task<IActionResult> ListarServicosLoja(
        Guid publicGuid,
        [FromQuery] Guid? profissionalPublicGuid,
        CancellationToken cancellationToken)
    {
        var servicos = await _servicoNegocioService.ListarPublicosPorEstabelecimentoAsync(
            publicGuid,
            profissionalPublicGuid,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ServicoPublicoResponseDto>>.From(
            "Servicos publicos da loja listados com sucesso.",
            servicos));
    }

    [HttpGet("profissional/{publicGuid:guid}/servicos")]
    public async Task<IActionResult> ListarServicosProfissional(
        Guid publicGuid,
        CancellationToken cancellationToken)
    {
        var servicos = await _servicoNegocioService.ListarPublicosPorProfissionalAsync(
            publicGuid,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ServicoPublicoResponseDto>>.From(
            "Servicos publicos do profissional listados com sucesso.",
            servicos));
    }

    [HttpPost("loja/{publicGuid:guid}")]
    public async Task<IActionResult> CriarAgendamentoLoja(
        Guid publicGuid,
        [FromBody] CriarAgendamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.CriarPublicoPorLojaAsync(
            publicGuid,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<AgendamentoCriadoResponseDto>.From(
                "Agendamento publico criado com sucesso.",
                agendamento));
    }

    [HttpPost("profissional/{publicGuid:guid}")]
    public async Task<IActionResult> CriarAgendamentoProfissional(
        Guid publicGuid,
        [FromBody] CriarAgendamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.CriarPublicoPorProfissionalAsync(
            publicGuid,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<AgendamentoCriadoResponseDto>.From(
                "Agendamento publico do profissional criado com sucesso.",
                agendamento));
    }
}
