using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IAssinaturaEstabelecimentoRepository : IRepository<AssinaturaEstabelecimento>
{
    Task<AssinaturaEstabelecimento?> ObterPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<int> ContarPorAssinaturaAsync(
        int assinaturaId,
        CancellationToken cancellationToken = default);

    Task<AssinaturaEstabelecimento?> ObterMatrizPorAssinaturaAsync(
        int assinaturaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssinaturaEstabelecimento>> ListarPorAssinaturaAsync(
        int assinaturaId,
        CancellationToken cancellationToken = default);
}
