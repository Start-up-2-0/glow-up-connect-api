using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IHorarioAtendimentoProfissionalRepository : IRepository<HorarioAtendimentoProfissional>
{
    Task<HorarioAtendimentoProfissional?> ObterPorIdEEstabelecimentoAsync(
        int horarioId,
        int estabelecimentoId,
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
