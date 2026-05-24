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

    public UsuarioController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> CriarUsuario([FromBody] CriarUsuarioDto request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioService.CriarUsuarioAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, UsuarioResponseDto.From(usuario));
    }

    [HttpGet("me")]
    public async Task<IActionResult> ObterUsuario(CancellationToken cancellationToken)
    {
        var usuario = await _usuarioService.ObterPerfilAtualAsync(cancellationToken);
        return Ok(UsuarioResponseDto.From(usuario));
    }

    [HttpPut("me")]
    public async Task<IActionResult> AtualizarUsuario([FromBody] AtualizarUsuarioDto request, CancellationToken cancellationToken)
    {
        await _usuarioService.AtualizarPerfilAtualAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DesativarUsuario(CancellationToken cancellationToken)
    {
        await _usuarioService.DesativarContaAtualAsync(cancellationToken);
        return NoContent();
    }
}
