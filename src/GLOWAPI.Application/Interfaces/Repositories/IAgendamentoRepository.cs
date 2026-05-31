using GLOWAPI.Application.Models.Agenda;
using GLOWAPI.Application.Models.Agendamento;
using GLOWAPI.Domain.Entities;

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

    Task<Agendamento?> ObterPorIdEUsuarioClienteAsync(
        int agendamentoId,
        int usuarioClienteId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Agendamento> Itens, int Total)> ListarPorUsuarioClienteComFiltroAsync(
        AgendamentoClienteFiltro filtro,
        CancellationToken cancellationToken = default);
}
