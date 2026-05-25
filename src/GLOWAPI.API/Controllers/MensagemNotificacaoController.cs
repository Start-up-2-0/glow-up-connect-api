using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/mensagens-notificacao")]
public class MensagemNotificacaoController : ControllerBase
{
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;

    public MensagemNotificacaoController(IMensagemNotificacaoService mensagemNotificacaoService)
    {
        _mensagemNotificacaoService = mensagemNotificacaoService;
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistrarMensagemNotificacaoDto request,
        CancellationToken cancellationToken)
    {
        var resultado = await _mensagemNotificacaoService.RegistrarAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    [HttpDelete("{guid:guid}")]
    public async Task<IActionResult> Cancelar(Guid guid, CancellationToken cancellationToken)
    {
        await _mensagemNotificacaoService.CancelarPorGuidAsync(guid, cancellationToken);
        return NoContent();
    }
}
