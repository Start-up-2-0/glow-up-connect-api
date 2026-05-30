using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Models.Agenda;
using GLOWAPI.Domain.Entities;

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

    Task<bool> ExisteAgendamentoFuturoImpactadoPorAlteracaoHorarioAsync(
        int estabelecimentoId,
        int profissionalId,
        DayOfWeek diaSemanaAtual,
        TimeOnly horaInicioAtual,
        TimeOnly horaFimAtual,
        DayOfWeek novoDiaSemana,
        TimeOnly novaHoraInicio,
        TimeOnly novaHoraFim,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgendamentoFuturoImpactadoDto>> ListarAgendamentosFuturosImpactadosPorAlteracaoHorarioAsync(
        int estabelecimentoId,
        int profissionalId,
        DayOfWeek diaSemanaAtual,
        TimeOnly horaInicioAtual,
        TimeOnly horaFimAtual,
        DayOfWeek novoDiaSemana,
        TimeOnly novaHoraInicio,
        TimeOnly novaHoraFim,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgendamentoFuturoImpactadoDto>> ListarAgendamentosFuturosNoHorarioAtivoAsync(
        int estabelecimentoId,
        int profissionalId,
        DayOfWeek diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgendamentoItem>> ListarOcupacaoAsync(
        int estabelecimentoId,
        int? profissionalId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteFuturoConfirmadoAsync(
        int profissionalId,
        int servicoId,
        CancellationToken cancellationToken = default);
}
