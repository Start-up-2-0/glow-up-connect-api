using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class ServicoRepository : Repository<Servico>, IServicoRepository
{
    public ServicoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Servico?> ObterPorIdEEstabelecimentoAsync(
        int servicoId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return ObterPorIdEEstabelecimentoAsync(servicoId, estabelecimentoId, ativo: true, cancellationToken);
    }

    public Task<Servico?> ObterPorIdEEstabelecimentoAsync(
        int servicoId,
        int estabelecimentoId,
        bool? ativo,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(servico => servico.Profissionais)
            .Where(servico => servico.Id == servicoId && servico.EstabelecimentoId == estabelecimentoId);

        if (ativo.HasValue)
        {
            query = query.Where(servico => servico.Ativo == ativo.Value);
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Servico>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        bool? ativo,
        int? profissionalId,
        string? nome,
        bool apenasVinculados = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(servico => servico.Profissionais)
            .Where(servico => servico.EstabelecimentoId == estabelecimentoId);

        if (ativo.HasValue)
        {
            query = query.Where(servico => servico.Ativo == ativo.Value);
        }

        if (profissionalId.HasValue)
        {
            var profId = profissionalId.Value;
            query = apenasVinculados
                ? query.Where(servico =>
                    servico.Profissionais.Any(vinculo =>
                        vinculo.ProfissionalId == profId && vinculo.Ativo))
                : query.Where(servico =>
                    !servico.Profissionais.Any(vinculo => vinculo.Ativo)
                    || servico.Profissionais.Any(vinculo =>
                        vinculo.ProfissionalId == profId && vinculo.Ativo));
        }

        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = nome.Trim();
            query = query.Where(servico => EF.Functions.ILike(servico.Nome, $"%{termo}%"));
        }

        return await query
            .OrderBy(servico => servico.Nome)
            .ThenBy(servico => servico.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Servico>> ListarPublicosPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(servico => servico.Profissionais)
            .Where(servico =>
                servico.EstabelecimentoId == estabelecimentoId
                && servico.Ativo
                && (!servico.Profissionais.Any(vinculo => vinculo.Ativo)
                    || servico.Profissionais.Any(vinculo => vinculo.Ativo)))
            .OrderBy(servico => servico.Nome)
            .ThenBy(servico => servico.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<int> ContarAtivosPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.CountAsync(
            servico => servico.EstabelecimentoId == estabelecimentoId && servico.Ativo,
            cancellationToken);
    }
}
