using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IHorarioAtendimentoProfissionalRepository : IRepository<HorarioAtendimentoProfissional>
{
    Task<IReadOnlyList<HorarioAtendimentoProfissional>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        int? profissionalId = null,
        DayOfWeek? diaSemana = null,
        bool? ativo = null,
        CancellationToken cancellationToken = default);

    Task<HorarioAtendimentoProfissional?> ObterPorIdEEstabelecimentoAsync(
        int horarioId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HorarioAtendimentoProfissional>> ListarAtivosPorEstabelecimentoEDiaAsync(
        int estabelecimentoId,
        DayOfWeek diaSemana,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteConflitoAtivoAsync(
        int profissionalId,
        int estabelecimentoId,
        DayOfWeek diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        int? ignorarHorarioId = null,
        CancellationToken cancellationToken = default);
}
