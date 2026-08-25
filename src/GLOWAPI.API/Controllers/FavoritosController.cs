using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Favoritos;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/favoritos")]
public class FavoritosController : ControllerBase
{
    private readonly IFavoritoClienteService _favoritoService;

    public FavoritosController(IFavoritoClienteService favoritoService) =>
        _favoritoService = favoritoService;

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var favoritos = await _favoritoService.ListarAsync(cancellationToken);
        return Ok(ApiSuccessResponse<IReadOnlyList<FavoritoClienteResponseDto>>.From(
            "Favoritos listados com sucesso.",
            favoritos));
    }

    [HttpPost]
    public async Task<IActionResult> Adicionar(
        [FromBody] CriarFavoritoClienteRequestDto request,
        CancellationToken cancellationToken)
    {
        var favorito = await _favoritoService.AdicionarAsync(request, cancellationToken);
        return Ok(ApiSuccessResponse<FavoritoClienteResponseDto>.From(
            "Favorito salvo com sucesso.",
            favorito));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken cancellationToken)
    {
        await _favoritoService.RemoverAsync(id, cancellationToken);
        return Ok(ApiSuccessResponse<object?>.From("Favorito removido com sucesso.", null));
    }
}
