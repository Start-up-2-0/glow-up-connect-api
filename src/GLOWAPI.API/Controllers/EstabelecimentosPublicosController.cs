using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Agendamento;
using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/publico/estabelecimentos")]
[AllowAnonymous]
public class EstabelecimentosPublicosController : ControllerBase
{
    private readonly IEstabelecimentoDescobertaService _estabelecimentoDescobertaService;
    private readonly IAgendamentoNegocioService _agendamentoNegocioService;

    public EstabelecimentosPublicosController(
        IEstabelecimentoDescobertaService estabelecimentoDescobertaService,
        IAgendamentoNegocioService agendamentoNegocioService)
    {
        _estabelecimentoDescobertaService = estabelecimentoDescobertaService;
        _agendamentoNegocioService = agendamentoNegocioService;
    }

    [HttpGet("proximos")]
    public async Task<IActionResult> ListarProximos(
        [FromQuery] decimal latitude,
        [FromQuery] decimal longitude,
        [FromQuery] double? raioKm,
        [FromQuery] int? categoriaId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _estabelecimentoDescobertaService.ListarProximosAsync(
            latitude,
            longitude,
            raioKm,
            pagina,
            tamanhoPagina,
            categoriaId,
            cancellationToken);

        return Ok(ApiSuccessResponse<EstabelecimentosProximosPaginadoResponseDto>.From(
            "Estabelecimentos proximos listados com sucesso.",
            resultado));
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> ListarCategorias(CancellationToken cancellationToken)
    {
        var categorias = await _estabelecimentoDescobertaService.ListarCategoriasAsync(cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<EstabelecimentoCategoriaDto>>.From(
            "Categorias de estabelecimento listadas com sucesso.",
            categorias));
    }

    [HttpGet("{publicGuid:guid}/profissionais-vitrine")]
    public async Task<IActionResult> ListarProfissionaisVitrine(
        Guid publicGuid,
        CancellationToken cancellationToken)
    {
        var profissionais = await _agendamentoNegocioService.ListarProfissionaisVitrinePorLojaAsync(
            publicGuid,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ProfissionalVitrinePublicoResponseDto>>.From(
            "Profissionais da vitrine listados com sucesso.",
            profissionais));
    }

    [HttpGet("{publicGuid:guid}")]
    public async Task<IActionResult> ObterPorPublicGuid(
        Guid publicGuid,
        [FromQuery] decimal? latitude,
        [FromQuery] decimal? longitude,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _estabelecimentoDescobertaService.ObterPorPublicGuidAsync(
            publicGuid,
            latitude,
            longitude,
            cancellationToken);

        return Ok(ApiSuccessResponse<EstabelecimentoPublicoResponseDto>.From(
            "Estabelecimento obtido com sucesso.",
            resultado));
    }
}
