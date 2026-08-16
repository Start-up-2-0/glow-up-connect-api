using GLOWAPI.API.Attributes;
using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Convites;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api")]
public class ConvitesController : ControllerBase
{
    private readonly IConviteNegocioService _conviteNegocioService;

    public ConvitesController(IConviteNegocioService conviteNegocioService)
    {
        _conviteNegocioService = conviteNegocioService;
    }

    [HttpPost("estabelecimentos/{estabelecimentoId:int}/convites")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.EquipeGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> CriarLink(
        int estabelecimentoId,
        [FromBody] CriarConviteLinkRequestDto request,
        CancellationToken cancellationToken)
    {
        var convite = await _conviteNegocioService.CriarLinkAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<ConviteNegocioCriadoResponseDto>.From(
                "Link de convite criado com sucesso.",
                convite));
    }

    [HttpGet("estabelecimentos/{estabelecimentoId:int}/convites")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.EquipeGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> Listar(
        int estabelecimentoId,
        [FromQuery] ConviteNegocioFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var convites = await _conviteNegocioService.ListarAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ConviteNegocioResponseDto>>.From(
            "Convites listados com sucesso.",
            convites));
    }

    [HttpDelete("estabelecimentos/{estabelecimentoId:int}/convites/{conviteId:int}")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.EquipeGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> Cancelar(
        int estabelecimentoId,
        int conviteId,
        CancellationToken cancellationToken)
    {
        var convite = await _conviteNegocioService.CancelarAsync(
            estabelecimentoId,
            conviteId,
            cancellationToken);

        return Ok(ApiSuccessResponse<ConviteNegocioResponseDto>.From(
            "Convite cancelado com sucesso.",
            convite));
    }
}
