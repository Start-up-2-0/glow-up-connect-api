using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Rede;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/rede")]
public class RedeController : ControllerBase
{
    private readonly IRedeNegocioService _redeNegocioService;
    private readonly ICurrentUserContext _currentUser;

    public RedeController(
        IRedeNegocioService redeNegocioService,
        ICurrentUserContext currentUser)
    {
        _redeNegocioService = redeNegocioService;
        _currentUser = currentUser;
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> ObterResumo(
        [FromQuery] int assinaturaId,
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var resumo = await _redeNegocioService.ObterResumoAsync(
            assinaturaId,
            _currentUser.UserId.Value,
            inicio,
            fim,
            cancellationToken);

        return Ok(ApiSuccessResponse<RedeResumoResponseDto>.From(
            "Resumo da rede obtido com sucesso.",
            resumo));
    }
}
