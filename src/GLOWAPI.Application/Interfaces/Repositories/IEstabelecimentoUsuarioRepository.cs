using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IEstabelecimentoUsuarioRepository : IRepository<EstabelecimentoUsuario>
{
    Task<EstabelecimentoUsuario?> ObterAtivoAsync(
        int estabelecimentoId,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
