using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Usuario;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/usuario")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly IUsuarioNegocioContextoService _usuarioNegocioContextoService;

    public UsuarioController(
        IUsuarioService usuarioService,
        IUsuarioNegocioContextoService usuarioNegocioContextoService)
    {
        _usuarioService = usuarioService;
        _usuarioNegocioContextoService = usuarioNegocioContextoService;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> CadastrarCliente([FromBody] CadastrarClienteDto request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioService.CadastrarClienteAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, CadastroClienteResponseDto.From(usuario));
    }

    [HttpGet("me")]
    public async Task<IActionResult> ObterUsuario(CancellationToken cancellationToken)
    {
        var usuario = await _usuarioService.ObterPerfilAtualAsync(cancellationToken);
        return Ok(UsuarioResponseDto.From(usuario));
    }

    [HttpGet("me/estabelecimentos")]
    public async Task<IActionResult> ListarEstabelecimentos(CancellationToken cancellationToken)
    {
        var estabelecimentos = await _usuarioNegocioContextoService.ListarEstabelecimentosAsync(cancellationToken);
        return Ok(estabelecimentos);
    }

    [HttpPut("me")]
    public async Task<IActionResult> AtualizarUsuario([FromBody] AtualizarUsuarioDto request, CancellationToken cancellationToken)
    {
        await _usuarioService.AtualizarPerfilAtualAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("me/whatsapp/solicitar-confirmacao")]
    public async Task<IActionResult> SolicitarConfirmacaoWhatsApp(CancellationToken cancellationToken)
    {
        var instrucoes = await _usuarioService.SolicitarConfirmacaoWhatsAppAtualAsync(cancellationToken);
        return Ok(ApiSuccessResponse<object>.From(
            "Verifique seu e-mail para confirmar o WhatsApp.",
            instrucoes));
    }

    [HttpPost("me/whatsapp/opt-in")]
    public async Task<IActionResult> AtualizarWhatsAppOptIn(
        [FromBody] WhatsAppOptInRequestDto request,
        CancellationToken cancellationToken)
    {
        await _usuarioService.AtualizarWhatsAppOptInAtualAsync(request.OptIn, cancellationToken);
        return NoContent();
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DesativarUsuario(CancellationToken cancellationToken)
    {
        await _usuarioService.DesativarContaAtualAsync(cancellationToken);
        return NoContent();
    }
}
