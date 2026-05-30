using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Agendamento;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/agendamentos")]
public class AgendamentosController : ControllerBase
{
    private readonly IAgendamentoNegocioService _agendamentoNegocioService;

    public AgendamentosController(IAgendamentoNegocioService agendamentoNegocioService)
    {
        _agendamentoNegocioService = agendamentoNegocioService;
    }

    [HttpPost]
    public async Task<IActionResult> CriarAgendamento(
        [FromBody] CriarAgendamentoLogadoRequestDto request,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.CriarLogadoAsync(request, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<AgendamentoCriadoResponseDto>.From(
                "Agendamento criado com sucesso.",
                agendamento));
    }

    [HttpGet("me")]
    public async Task<IActionResult> ListarMeusAgendamentos(CancellationToken cancellationToken)
    {
        var agendamentos = await _agendamentoNegocioService.ListarMeusAgendamentosAsync(cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<AgendamentoCriadoResponseDto>>.From(
            "Agendamentos do cliente listados com sucesso.",
            agendamentos));
    }
}
