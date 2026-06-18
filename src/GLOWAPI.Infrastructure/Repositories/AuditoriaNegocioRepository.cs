using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class AuditoriaNegocioRepository : Repository<AuditoriaNegocio>, IAuditoriaNegocioRepository
{
    public AuditoriaNegocioRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<AuditoriaNegocio>> ListarRecentesPorEstabelecimentoAsync(
        int estabelecimentoId,
        int limite,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(auditoria => auditoria.EstabelecimentoId == estabelecimentoId)
            .OrderByDescending(auditoria => auditoria.CriadoEm)
            .Take(limite)
            .ToListAsync(cancellationToken);
    }
}
