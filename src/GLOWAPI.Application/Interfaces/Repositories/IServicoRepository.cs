using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IServicoRepository : IRepository<Servico>
{
    Task<Servico?> ObterPorIdEEstabelecimentoAsync(
        int servicoId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<Servico?> ObterPorIdEEstabelecimentoAsync(
        int servicoId,
        int estabelecimentoId,
        bool? ativo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Servico>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        bool? ativo,
        int? profissionalId,
        string? nome,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Servico>> ListarPublicosPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<int> ContarAtivosPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);
}
