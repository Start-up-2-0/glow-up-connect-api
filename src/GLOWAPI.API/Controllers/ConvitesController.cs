using GLOWAPI.API.Attributes;
using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Convites;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
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
            ApiSuccessResponse<ConviteOuVinculoResponseDto>.From(
                "Convite enviado com sucesso.",
                convite));
    }

    [HttpPost("estabelecimentos/{estabelecimentoId:int}/convites/usuarios")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.EquipeGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> CriarConviteUsuarioEquipe(
        int estabelecimentoId,
        [FromBody] CriarConviteUsuarioEquipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var convite = await _conviteNegocioService.CriarConviteUsuarioEquipeAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<ConviteOuVinculoResponseDto>.From(
                "Convite enviado com sucesso.",
                convite));
    }

    [AllowAnonymous]
    [HttpGet("convites/{token}/preview")]
    public async Task<IActionResult> ObterPreview(
        string token,
        CancellationToken cancellationToken)
    {
        var preview = await _conviteNegocioService.ObterPreviewAsync(token, cancellationToken);

        return Ok(ApiSuccessResponse<ConviteNegocioPreviewResponseDto>.From(
            "Convite encontrado.",
            preview));
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
