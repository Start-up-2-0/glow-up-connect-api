using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IAvaliacaoAtendimentoRepository : IRepository<AvaliacaoAtendimento>
{
    Task<AvaliacaoAtendimento?> ObterPorAgendamentoIdAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, AvaliacaoAtendimento>> ObterPorAgendamentoIdsAsync(
        IReadOnlyCollection<int> agendamentoIds,
        CancellationToken cancellationToken = default);

    Task<AvaliacaoAtendimento?> ObterPorAgendamentoIdComDetalhesAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvaliacaoAtendimento>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        DateTime avaliadoDesde,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default);

    Task<int> ContarPorEstabelecimentoAsync(
        int estabelecimentoId,
        DateTime avaliadoDesde,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvaliacaoAtendimento>> ListarPorProfissionalAsync(
        int profissionalId,
        DateTime avaliadoDesde,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default);

    Task<int> ContarPorProfissionalAsync(
        int profissionalId,
        DateTime avaliadoDesde,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<byte>> ListarNotasEstabelecimentoAsync(
        int estabelecimentoId,
        DateTime avaliadoDesde,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<byte>> ListarNotasProfissionalAsync(
        int profissionalId,
        DateTime avaliadoDesde,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> ListarEstabelecimentosComAvaliacoesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> ListarProfissionaisComAvaliacoesAsync(
        CancellationToken cancellationToken = default);
}
