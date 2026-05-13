using GLOWAPI.API.DTOs.Usuario;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuarioController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    // POST: api/Usuario
    [HttpPost]
    [AllowAnonymous]
    //[Authorize]
    public async Task<IActionResult> CriarUsuario([FromBody] CriarUsuarioDto request)
    {
        var usuario = await _usuarioService.CriarUsuarioAsync(
            request.Nome,
            request.Email,
            request.Telefone,
            request.Senha,
            request.Role);

        return CreatedAtAction(nameof(ObterUsuario), new { id = usuario.Id }, usuario);
    }

    // GET: api/Usuario/
    [HttpGet("{id}")]
    // [Authorize]
    public async Task<IActionResult> ObterUsuario(int id)
    {
        var usuario = await _usuarioService.ObterUsuarioPorIdAsync(id);
        if (usuario == null || !usuario.Ativo)
        {
            return NotFound();
        }

        return Ok(usuario);
    }

    // PUT: api/Usuario
    [HttpPut("{id}")]
    //[Authorize]
    public async Task<IActionResult> AtualizarUsuario(int id, [FromBody] AtualizarUsuarioDto request)
    {
        await _usuarioService.AtualizarUsuarioAsync(id, request.Nome, request.Telefone);
        return NoContent();
    }


    // DELETE: api/Usuario/
    [HttpDelete("{id}")]
    //[Authorize]
    public async Task<IActionResult> DesativarUsuario(int id)
    {
        await _usuarioService.DesativarUsuarioAsync(id);
        return NoContent();
    }

}