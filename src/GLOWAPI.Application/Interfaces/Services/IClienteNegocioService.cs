using GLOWAPI.Application.DTOs.Clientes;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IClienteNegocioService
{
    Task<IReadOnlyList<ClienteNegocioResponseDto>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);
}
