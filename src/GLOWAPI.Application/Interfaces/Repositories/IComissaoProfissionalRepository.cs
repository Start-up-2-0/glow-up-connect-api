using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IComissaoProfissionalRepository : IRepository<ComissaoProfissional>
{
    Task<IReadOnlyList<ComissaoProfissional>> ListarAtivasPorVinculosAsync(
        IReadOnlyList<int> profissionalEstabelecimentoIds,
        CancellationToken cancellationToken = default);

    Task<ComissaoProfissional?> ObterAtivaPorVinculoAsync(
        int profissionalEstabelecimentoId,
        DateTime referenciaUtc,
        CancellationToken cancellationToken = default);

    Task<ComissaoProfissional?> ObterPorIdComTrackingAsync(
        int comissaoId,
        CancellationToken cancellationToken = default);
}
