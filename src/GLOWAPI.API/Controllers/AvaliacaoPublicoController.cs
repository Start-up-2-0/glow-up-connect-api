using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Avaliacao;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/publico/avaliacoes")]
[AllowAnonymous]
public class AvaliacaoPublicoController : ControllerBase
{
    private readonly IAvaliacaoAtendimentoService _avaliacaoAtendimentoService;
    private readonly IAvaliacaoResumoService _avaliacaoResumoService;

    public AvaliacaoPublicoController(
        IAvaliacaoAtendimentoService avaliacaoAtendimentoService,
        IAvaliacaoResumoService avaliacaoResumoService)
    {
        _avaliacaoAtendimentoService = avaliacaoAtendimentoService;
        _avaliacaoResumoService = avaliacaoResumoService;
    }

    [HttpGet("{token:guid}")]
    public async Task<IActionResult> ObterContexto(Guid token, CancellationToken cancellationToken)
    {
        var contexto = await _avaliacaoAtendimentoService.ObterContextoPorTokenAsync(token, cancellationToken);

        return Ok(ApiSuccessResponse<AvaliacaoContextoResponseDto>.From(
            "Contexto de avaliacao obtido com sucesso.",
            contexto));
    }

    [HttpPost("{token:guid}")]
    public async Task<IActionResult> CriarAvaliacao(
        Guid token,
        [FromBody] CriarAvaliacaoAtendimentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var contexto = await _avaliacaoAtendimentoService.CriarPorTokenAsync(token, request, cancellationToken);

        return Ok(ApiSuccessResponse<AvaliacaoContextoResponseDto>.From(
            "Avaliacao registrada com sucesso.",
            contexto));
    }

    [HttpGet("estabelecimentos/{publicGuid:guid}")]
    public async Task<IActionResult> ListarEstabelecimento(
        Guid publicGuid,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _avaliacaoResumoService.ListarEstabelecimentoPublicoAsync(
            publicGuid,
            pagina,
            tamanhoPagina,
            cancellationToken);

        return Ok(ApiSuccessResponse<AvaliacoesPaginadasResponseDto>.From(
            "Avaliacoes do estabelecimento listadas com sucesso.",
            resultado));
    }

    [HttpGet("profissionais/{publicGuid:guid}")]
    public async Task<IActionResult> ListarProfissional(
        Guid publicGuid,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _avaliacaoResumoService.ListarProfissionalPublicoAsync(
            publicGuid,
            pagina,
            tamanhoPagina,
            cancellationToken);

        return Ok(ApiSuccessResponse<AvaliacoesPaginadasResponseDto>.From(
            "Avaliacoes do profissional listadas com sucesso.",
            resultado));
    }
}
