using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class ProfissionalServicoRepository : Repository<ProfissionalServico>, IProfissionalServicoRepository
{
    public ProfissionalServicoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<ProfissionalServico?> ObterPorProfissionalEServicoAsync(
        int profissionalId,
        int servicoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .FirstOrDefaultAsync(
                ps => ps.ProfissionalId == profissionalId && ps.ServicoId == servicoId,
                cancellationToken);
    }

    public Task<bool> ExisteAtivoAsync(
        int profissionalId,
        int servicoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            ps => ps.ProfissionalId == profissionalId
                && ps.ServicoId == servicoId
                && ps.Ativo,
            cancellationToken);
    }
}
