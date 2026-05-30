using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class HorarioFuncionamentoEstabelecimentoRepository : Repository<HorarioFuncionamentoEstabelecimento>, IHorarioFuncionamentoEstabelecimentoRepository
{
    public HorarioFuncionamentoEstabelecimentoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<HorarioFuncionamentoEstabelecimento?> ObterPorIdEEstabelecimentoAsync(
        int horarioId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            horario => horario.Id == horarioId && horario.EstabelecimentoId == estabelecimentoId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<HorarioFuncionamentoEstabelecimento>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        DayOfWeek? diaSemana = null,
        bool? ativo = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(horario => horario.EstabelecimentoId == estabelecimentoId);

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
            .ThenBy(horario => horario.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HorarioFuncionamentoEstabelecimento>> ListarAtivosPorEstabelecimentoEDiaAsync(
        int estabelecimentoId,
        DayOfWeek diaSemana,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(horario => horario.EstabelecimentoId == estabelecimentoId
                && horario.DiaSemana == diaSemana
                && horario.Ativo)
            .OrderBy(horario => horario.HoraInicio)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExisteConflitoAtivoAsync(
        int estabelecimentoId,
        DayOfWeek diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        int? ignorarHorarioId = null,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            horario => horario.EstabelecimentoId == estabelecimentoId
                && horario.DiaSemana == diaSemana
                && horario.Ativo
                && (!ignorarHorarioId.HasValue || horario.Id != ignorarHorarioId.Value)
                && horario.HoraInicio < horaFim
                && horaInicio < horario.HoraFim,
            cancellationToken);
    }
}
