using GLOWAPI.Domain.Entities;
using GLOWAPI.Application.Models.Caixa;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface ILancamentoCaixaRepository : IRepository<LancamentoCaixa>
{
    Task<IReadOnlyList<LancamentoCaixa>> ListarPorCaixaAsync(
        LancamentoCaixaFiltro filtro,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<LancamentoCaixa> Itens, int Total)> ListarPorCaixaPaginadoAsync(
        LancamentoCaixaFiltro filtro,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LancamentoCaixa>> ListarTodosPorCaixaAsync(
        int caixaId,
        CancellationToken cancellationToken = default);

    Task<LancamentoCaixa?> ObterPorIdECaixaAsync(
        int lancamentoId,
        int caixaId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteLancamentoAtivoPorAgendamentoETipoAsync(
        int agendamentoId,
        LancamentoCaixaTipo tipo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LancamentoCaixa>> ListarPorProfissionalAsync(
        int caixaId,
        int profissionalId,
        DateTime? inicio,
        DateTime? fim,
        CancellationToken cancellationToken = default);
}
