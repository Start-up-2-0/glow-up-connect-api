using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Agendamento;
using GLOWAPI.Application.DTOs.Avaliacao;
using GLOWAPI.Application.DTOs.Dashboard;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/agendamentos")]
public class AgendamentosController : ControllerBase
{
    private readonly IAgendamentoNegocioService _agendamentoNegocioService;
    private readonly IAvaliacaoAtendimentoService _avaliacaoAtendimentoService;
    private readonly IDashboardClienteService _dashboardClienteService;

    public AgendamentosController(
        IAgendamentoNegocioService agendamentoNegocioService,
        IAvaliacaoAtendimentoService avaliacaoAtendimentoService,
        IDashboardClienteService dashboardClienteService)
    {
        _agendamentoNegocioService = agendamentoNegocioService;
        _avaliacaoAtendimentoService = avaliacaoAtendimentoService;
        _dashboardClienteService = dashboardClienteService;
    }

    [HttpPost]
    public async Task<IActionResult> CriarAgendamento(
        [FromBody] CriarAgendamentoLogadoRequestDto request,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.CriarLogadoAsync(request, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<AgendamentoClienteResponseDto>.From(
                "Agendamento criado com sucesso.",
                agendamento));
    }

    [HttpGet("me/dashboard")]
    public async Task<IActionResult> ObterDashboardCliente(CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardClienteService.ObterAsync(cancellationToken);

        return Ok(ApiSuccessResponse<DashboardClienteResponseDto>.From(
            "Dashboard do cliente obtido com sucesso.",
            dashboard));
    }

    [HttpGet("me")]
    public async Task<IActionResult> ListarMeusAgendamentos(
        [FromQuery] AgendamentoClienteFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var agendamentos = await _agendamentoNegocioService.ListarMeusAgendamentosAsync(filtro, cancellationToken);

        return Ok(ApiSuccessResponse<AgendamentosClientePaginadoResponseDto>.From(
            "Agendamentos do cliente listados com sucesso.",
            agendamentos));
    }

    [HttpGet("me/{id:int}")]
    public async Task<IActionResult> ObterMeuAgendamento(int id, CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.ObterMeuAgendamentoAsync(id, cancellationToken);

        return Ok(ApiSuccessResponse<AgendamentoClienteResponseDto>.From(
            "Agendamento obtido com sucesso.",
            agendamento));
    }

    [HttpPost("me/{id:int}/cancelar")]
    public async Task<IActionResult> CancelarMeuAgendamento(
        int id,
        [FromBody] CancelarAgendamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.CancelarMeuAgendamentoAsync(id, request, cancellationToken);

        return Ok(ApiSuccessResponse<AgendamentoClienteResponseDto>.From(
            "Agendamento cancelado com sucesso.",
            agendamento));
    }

    [HttpPost("me/{id:int}/remarcar")]
    public async Task<IActionResult> RemarcarMeuAgendamento(
        int id,
        [FromBody] RemarcarAgendamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.RemarcarMeuAgendamentoAsync(id, request, cancellationToken);

        return Ok(ApiSuccessResponse<AgendamentoClienteResponseDto>.From(
            "Agendamento remarcado com sucesso.",
            agendamento));
    }

    [HttpPost("me/{id:int}/propostas-remarcacao/{propostaId:int}/aceitar")]
    public async Task<IActionResult> AceitarPropostaRemarcacao(
        int id,
        int propostaId,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.AceitarPropostaRemarcacaoLogadoAsync(
            id,
            propostaId,
            cancellationToken);

        return Ok(ApiSuccessResponse<AgendamentoClienteResponseDto>.From(
            "Proposta de remarcacao aceita com sucesso.",
            agendamento));
    }

    [HttpGet("me/{id:int}/avaliacao")]
    public async Task<IActionResult> ObterAvaliacaoMeuAgendamento(int id, CancellationToken cancellationToken)
    {
        var contexto = await _avaliacaoAtendimentoService.ObterContextoMeuAgendamentoAsync(id, cancellationToken);

        return Ok(ApiSuccessResponse<AvaliacaoContextoResponseDto>.From(
            "Contexto de avaliacao obtido com sucesso.",
            contexto));
    }

    [HttpPost("me/{id:int}/avaliacao")]
    public async Task<IActionResult> CriarAvaliacaoMeuAgendamento(
        int id,
        [FromBody] CriarAvaliacaoAtendimentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var contexto = await _avaliacaoAtendimentoService.CriarMeuAgendamentoAsync(id, request, cancellationToken);

        return Ok(ApiSuccessResponse<AvaliacaoContextoResponseDto>.From(
            "Avaliacao registrada com sucesso.",
            contexto));
    }
}
