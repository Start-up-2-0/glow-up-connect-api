using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class HorarioFuncionamentoEstabelecimentoRepository : Repository<HorarioFuncionamentoEstabelecimento>, IHorarioFuncionamentoEstabelecimentoRepository
{
    public HorarioFuncionamentoEstabelecimentoRepository(ApplicationDbContext context) : base(context)
    {
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
}
