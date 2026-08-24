using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IFavoritoClienteRepository : IRepository<FavoritoCliente>
{
    Task<IReadOnlyList<FavoritoCliente>> ListarPorUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<FavoritoCliente?> ObterAsync(int usuarioId, int estabelecimentoId, int? profissionalId, CancellationToken cancellationToken = default);
    Task<FavoritoCliente?> ObterPorIdAsync(int id, int usuarioId, CancellationToken cancellationToken = default);
}
