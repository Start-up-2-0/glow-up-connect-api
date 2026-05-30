using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IProfissionalEstabelecimentoRepository : IRepository<ProfissionalEstabelecimento>
{
    Task<ProfissionalEstabelecimento?> ObterAtivoPorProfissionalAsync(
        int profissionalId,
        CancellationToken cancellationToken = default);

    Task<ProfissionalEstabelecimento?> ObterAtivoPorUsuarioAsync(
        int usuarioId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteAtivoAsync(
        int profissionalId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<ProfissionalEstabelecimento?> ObterPorProfissionalAsync(
        int profissionalId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteAtivoPorUsuarioAsync(
        int usuarioId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<int> ContarAtivosPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfissionalEstabelecimento>> ListarAtivosComAgendamentoPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);
}
