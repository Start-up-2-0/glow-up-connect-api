using GLOWAPI.API.Models;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/privacidade")]
public class PrivacidadeController : ControllerBase
{
    private readonly IPrivacidadeTitularService _privacidadeTitularService;

    public PrivacidadeController(IPrivacidadeTitularService privacidadeTitularService)
    {
        _privacidadeTitularService = privacidadeTitularService;
    }

    [HttpGet("meus-dados")]
    public async Task<IActionResult> ExportarMeusDados(CancellationToken cancellationToken)
    {
        var dados = await _privacidadeTitularService.ExportarMeusDadosAsync(cancellationToken);
        return Ok(dados);
    }

    [HttpPost("solicitar-exclusao")]
    public async Task<IActionResult> SolicitarExclusao(CancellationToken cancellationToken)
    {
        await _privacidadeTitularService.SolicitarExclusaoAsync(cancellationToken);
        return Ok(ApiSuccessResponse.From(
            "Solicitacao de exclusao registrada. Nossa equipe entrara em contato conforme prazos legais."));
    }

    [HttpPost("revogar-consentimento")]
    public async Task<IActionResult> RevogarConsentimento(CancellationToken cancellationToken)
    {
        await _privacidadeTitularService.RevogarConsentimentoAsync(cancellationToken);
        return Ok(ApiSuccessResponse.From("Consentimento revogado para comunicacoes opcionais."));
    }
}
