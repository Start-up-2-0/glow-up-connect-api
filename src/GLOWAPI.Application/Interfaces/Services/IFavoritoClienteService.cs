using GLOWAPI.Application.DTOs.Favoritos;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IFavoritoClienteService
{
    Task<IReadOnlyList<FavoritoClienteResponseDto>> ListarAsync(CancellationToken cancellationToken = default);
    Task<FavoritoClienteResponseDto> AdicionarAsync(CriarFavoritoClienteRequestDto request, CancellationToken cancellationToken = default);
    Task RemoverAsync(int id, CancellationToken cancellationToken = default);
}
