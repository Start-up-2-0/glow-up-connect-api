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

    [HttpPost("estabelecimentos/{estabelecimentoId:int}/convites/profissionais")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ProfissionalConvidar, "estabelecimentoId")]
    public async Task<IActionResult> CriarConviteProfissional(
        int estabelecimentoId,
        [FromBody] CriarConviteProfissionalRequestDto request,
        CancellationToken cancellationToken)
    {
        var convite = await _conviteNegocioService.CriarConviteProfissionalAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<ConviteNegocioResponseDto>.From(
                "Convite enviado com sucesso.",
                convite));
    }

    [HttpPost("convites/{token}/aceitar")]
    public async Task<IActionResult> Aceitar(
        string token,
        CancellationToken cancellationToken)
    {
        var convite = await _conviteNegocioService.AceitarAsync(token, cancellationToken);

        return Ok(ApiSuccessResponse<ConviteNegocioResponseDto>.From(
            "Convite aceito com sucesso.",
            convite));
    }

    [HttpPost("convites/{token}/rejeitar")]
    public async Task<IActionResult> Rejeitar(
        string token,
        CancellationToken cancellationToken)
    {
        var convite = await _conviteNegocioService.RejeitarAsync(token, cancellationToken);

        return Ok(ApiSuccessResponse<ConviteNegocioResponseDto>.From(
            "Convite rejeitado com sucesso.",
            convite));
    }

    [HttpDelete("estabelecimentos/{estabelecimentoId:int}/convites/{conviteId:int}")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ProfissionalGerenciar, "estabelecimentoId")]
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
