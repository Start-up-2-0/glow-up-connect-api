using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class HorarioAtendimentoProfissionalRepository : Repository<HorarioAtendimentoProfissional>, IHorarioAtendimentoProfissionalRepository
{
    public HorarioAtendimentoProfissionalRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<HorarioAtendimentoProfissional?> ObterPorIdEEstabelecimentoAsync(
        int horarioId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            horario => horario.Id == horarioId && horario.EstabelecimentoId == estabelecimentoId,
            cancellationToken);
    }

    public Task<bool> ExisteConflitoAtivoAsync(
        int profissionalId,
        int estabelecimentoId,
        DayOfWeek diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        int? ignorarHorarioId = null,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            horario => horario.ProfissionalId == profissionalId
                && horario.EstabelecimentoId == estabelecimentoId
                && horario.DiaSemana == diaSemana
                && horario.Ativo
                && (!ignorarHorarioId.HasValue || horario.Id != ignorarHorarioId.Value)
                && horario.HoraInicio < horaFim
                && horaInicio < horario.HoraFim,
            cancellationToken);
    }
}
