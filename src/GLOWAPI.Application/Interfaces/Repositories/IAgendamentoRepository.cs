using GLOWAPI.Domain.Entities;
using GLOWAPI.Application.Models.Agenda;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IAgendamentoRepository : IRepository<Agendamento>
{
    Task<IReadOnlyList<Agendamento>> ListarAgendaGeralAsync(
        AgendaGeralFiltro filtro,
        CancellationToken cancellationToken = default);

    Task<Agendamento?> ObterPorIdEEstabelecimentoComItensAsync(
        int agendamentoId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Agendamento>> ListarPorUsuarioClienteAsync(
        int usuarioClienteId,
        CancellationToken cancellationToken = default);
}
