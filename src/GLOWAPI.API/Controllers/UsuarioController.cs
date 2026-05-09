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
    public async Task<IActionResult> CriarUsuario([FromBody] CriarUsuarioRequest request)
    {
        try
        {
            var usuario = await _usuarioService.CriarUsuarioAsync(
                request.Nome,
                request.Email,
                request.Telefone,
                request.Senha,
                request.Role);

            return CreatedAtAction(nameof(ObterUsuario), new { id = usuario.Id }, usuario);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // GET: api/Usuario/
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> ObterUsuario(int id)
    {
        var usuario = await _usuarioService.ObterUsuarioPorIdAsync(id);
        if (usuario == null || !usuario.Ativo)
        {
            return NotFound();
        }

        return Ok(usuario);
    }

    // PUT: api/Usuario/
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> AtualizarUsuario(int id, [FromBody] AtualizarUsuarioRequest request)
    {
        try
        {
            await _usuarioService.AtualizarUsuarioAsync(id, request.Nome, request.Telefone);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // DELETE: api/Usuario/
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DesativarUsuario(int id)
    {
        try
        {
            await _usuarioService.DesativarUsuarioAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public class CriarUsuarioRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}

public class AtualizarUsuarioRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
}