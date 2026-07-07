using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Models.Caixa;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class LancamentoCaixaRepository : Repository<LancamentoCaixa>, ILancamentoCaixaRepository
{
    public LancamentoCaixaRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<LancamentoCaixa>> ListarPorCaixaAsync(
        LancamentoCaixaFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(lancamento => lancamento.CaixaId == filtro.CaixaId);

        if (filtro.Inicio.HasValue)
        {
            query = query.Where(lancamento => lancamento.CreateAd >= filtro.Inicio.Value);
        }

        if (filtro.Fim.HasValue)
        {
            query = query.Where(lancamento => lancamento.CreateAd < filtro.Fim.Value);
        }

        return await query
            .OrderByDescending(lancamento => lancamento.CreateAd)
            .ThenByDescending(lancamento => lancamento.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LancamentoCaixa>> ListarTodosPorCaixaAsync(
        int caixaId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(lancamento => lancamento.CaixaId == caixaId)
            .OrderBy(lancamento => lancamento.CreateAd)
            .ThenBy(lancamento => lancamento.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<LancamentoCaixa?> ObterPorIdECaixaAsync(
        int lancamentoId,
        int caixaId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            lancamento => lancamento.Id == lancamentoId && lancamento.CaixaId == caixaId,
            cancellationToken);
    }

    public Task<bool> ExisteLancamentoAtivoPorAgendamentoETipoAsync(
        int agendamentoId,
        LancamentoCaixaTipo tipo,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            lancamento => lancamento.AgendamentoId == agendamentoId && lancamento.Tipo == tipo,
            cancellationToken);
    }

    public async Task<IReadOnlyList<LancamentoCaixa>> ListarPorProfissionalAsync(
        int caixaId,
        int profissionalId,
        DateTime? inicio,
        DateTime? fim,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(l => l.CaixaId == caixaId
                && l.ProfissionalId == profissionalId
                && l.Tipo == LancamentoCaixaTipo.ComissaoProfissional);

        if (inicio.HasValue)
        {
            query = query.Where(l => l.CreateAd >= inicio.Value);
        }

        if (fim.HasValue)
        {
            query = query.Where(l => l.CreateAd < fim.Value);
        }

        return await query
            .OrderByDescending(l => l.CreateAd)
            .ToListAsync(cancellationToken);
    }
}
