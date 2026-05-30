using GLOWAPI.Application.DTOs.Usuario;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IUsuarioNegocioContextoService
{
    Task<IReadOnlyList<EstabelecimentoAcessoResponseDto>> ListarEstabelecimentosAsync(
        CancellationToken cancellationToken = default);
}
