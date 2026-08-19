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

    public async Task<IReadOnlyList<int>> ListarProfissionaisAtivosPorServicoAsync(
        int servicoId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(ps => ps.ServicoId == servicoId && ps.Ativo)
            .Select(ps => ps.ProfissionalId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExisteVinculoAtivoPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            ps => ps.Ativo
                && ps.Servico != null
                && ps.Servico.Ativo
                && ps.Servico.EstabelecimentoId == estabelecimentoId
                && ps.Profissional != null
                && ps.Profissional.Ativo
                && ps.Profissional.Estabelecimentos.Any(vinculo =>
                    vinculo.EstabelecimentoId == estabelecimentoId
                    && vinculo.Ativo
                    && vinculo.PodeReceberAgendamento),
            cancellationToken);
    }
}
