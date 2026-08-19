using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IProfissionalServicoRepository : IRepository<ProfissionalServico>
{
    Task<ProfissionalServico?> ObterPorProfissionalEServicoAsync(
        int profissionalId,
        int servicoId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteAtivoAsync(
        int profissionalId,
        int servicoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> ListarProfissionaisAtivosPorServicoAsync(
        int servicoId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteVinculoAtivoPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);
}
