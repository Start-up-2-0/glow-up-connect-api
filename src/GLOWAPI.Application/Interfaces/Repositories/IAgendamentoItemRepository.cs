using GLOWAPI.Domain.Entities;
using GLOWAPI.Application.Models.Agenda;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IAgendamentoItemRepository : IRepository<AgendamentoItem>
{
    Task<IReadOnlyList<AgendamentoItem>> ListarAgendaProfissionalAsync(
        AgendaProfissionalFiltro filtro,
        CancellationToken cancellationToken = default);

    Task<AgendamentoItem?> ObterPorIdComAgendamentoAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteClienteVinculadoAoProfissionalAsync(
        int estabelecimentoId,
        int profissionalId,
        int usuarioClienteId,
        CancellationToken cancellationToken = default);
}
