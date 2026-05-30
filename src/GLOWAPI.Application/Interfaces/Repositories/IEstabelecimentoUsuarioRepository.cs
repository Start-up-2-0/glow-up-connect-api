using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IEstabelecimentoUsuarioRepository : IRepository<EstabelecimentoUsuario>
{
    Task<EstabelecimentoUsuario?> ObterAtivoAsync(
        int estabelecimentoId,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<EstabelecimentoUsuario?> ObterPorUsuarioAsync(
        int estabelecimentoId,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<int> ContarAtivosAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<int> ContarOwnersAtivosAsync(
        int estabelecimentoId,
        int? ignorarUsuarioId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EstabelecimentoUsuario>> ListarAtivosPorUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);
}
