using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IProfissionalEstabelecimentoRepository : IRepository<ProfissionalEstabelecimento>
{
    Task<ProfissionalEstabelecimento?> ObterAtivoPorProfissionalAsync(
        int profissionalId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteAtivoAsync(
        int profissionalId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default);
}
