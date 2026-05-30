using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class HorarioAtendimentoProfissionalRepository : Repository<HorarioAtendimentoProfissional>, IHorarioAtendimentoProfissionalRepository
{
    public HorarioAtendimentoProfissionalRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<HorarioAtendimentoProfissional>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        int? profissionalId = null,
        DayOfWeek? diaSemana = null,
        bool? ativo = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(horario => horario.EstabelecimentoId == estabelecimentoId);

        if (profissionalId.HasValue)
        {
            query = query.Where(horario => horario.ProfissionalId == profissionalId.Value);
        }

        if (diaSemana.HasValue)
        {
            query = query.Where(horario => horario.DiaSemana == diaSemana.Value);
        }

        if (ativo.HasValue)
        {
            query = query.Where(horario => horario.Ativo == ativo.Value);
        }

        return await query
            .OrderBy(horario => horario.DiaSemana)
            .ThenBy(horario => horario.HoraInicio)
            .ThenBy(horario => horario.ProfissionalId)
            .ThenBy(horario => horario.Id)
            .ToListAsync(cancellationToken);
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

    public async Task<IReadOnlyList<HorarioAtendimentoProfissional>> ListarAtivosPorEstabelecimentoEDiaAsync(
        int estabelecimentoId,
        DayOfWeek diaSemana,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(horario => horario.EstabelecimentoId == estabelecimentoId
                && horario.DiaSemana == diaSemana
                && horario.Ativo)
            .OrderBy(horario => horario.ProfissionalId)
            .ThenBy(horario => horario.HoraInicio)
            .ToListAsync(cancellationToken);
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
