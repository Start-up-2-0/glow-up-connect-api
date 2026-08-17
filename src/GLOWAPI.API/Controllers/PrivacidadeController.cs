using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Privacidade;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/privacidade")]
public class PrivacidadeController : ControllerBase
{
    private readonly IPrivacidadeTitularService _privacidadeTitularService;
    private readonly IExclusaoContaService _exclusaoContaService;

    public PrivacidadeController(
        IPrivacidadeTitularService privacidadeTitularService,
        IExclusaoContaService exclusaoContaService)
    {
        _privacidadeTitularService = privacidadeTitularService;
        _exclusaoContaService = exclusaoContaService;
    }

    [HttpGet("meus-dados")]
    public async Task<IActionResult> ExportarMeusDados(CancellationToken cancellationToken)
    {
        var dados = await _privacidadeTitularService.ExportarMeusDadosAsync(cancellationToken);
        return Ok(dados);
    }

    [HttpPost("solicitar-exclusao")]
    public async Task<IActionResult> SolicitarExclusao(
        [FromBody] SolicitarExclusaoContaRequestDto request,
        CancellationToken cancellationToken)
    {
        await _exclusaoContaService.SolicitarAsync(request.Senha, cancellationToken);
        return Ok(ApiSuccessResponse.From(
            "Exclusao solicitada. Voce tem 30 dias para reativar a conta."));
    }

    [HttpPost("revogar-consentimento")]
    public async Task<IActionResult> RevogarConsentimento(CancellationToken cancellationToken)
    {
        await _privacidadeTitularService.RevogarConsentimentoAsync(cancellationToken);
        return Ok(ApiSuccessResponse.From("Consentimento revogado para comunicacoes opcionais."));
    }
}
