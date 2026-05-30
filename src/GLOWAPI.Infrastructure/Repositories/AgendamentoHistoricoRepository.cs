using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class AgendamentoHistoricoRepository : Repository<AgendamentoHistorico>, IAgendamentoHistoricoRepository
{
    public AgendamentoHistoricoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<AgendamentoHistorico>> ListarPorAgendamentoAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(historico => historico.UsuarioExecutor)
            .Where(historico => historico.AgendamentoId == agendamentoId)
            .OrderBy(historico => historico.CriadoEm)
            .ThenBy(historico => historico.Id)
            .ToListAsync(cancellationToken);
    }
}
