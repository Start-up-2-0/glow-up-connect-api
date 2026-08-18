using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class ComissaoProfissionalRepository : Repository<ComissaoProfissional>, IComissaoProfissionalRepository
{
    public ComissaoProfissionalRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ComissaoProfissional>> ListarAtivasPorVinculosAsync(
        IReadOnlyList<int> profissionalEstabelecimentoIds,
        CancellationToken cancellationToken = default)
    {
        if (profissionalEstabelecimentoIds.Count == 0)
        {
            return Array.Empty<ComissaoProfissional>();
        }

        return await DbSet
            .AsNoTracking()
            .Where(comissao =>
                profissionalEstabelecimentoIds.Contains(comissao.ProfissionalEstabelecimentoId)
                && comissao.Ativo)
            .OrderBy(comissao => comissao.ProfissionalEstabelecimentoId)
            .ToListAsync(cancellationToken);
    }

    public Task<ComissaoProfissional?> ObterAtivaPorVinculoAsync(
        int profissionalEstabelecimentoId,
        DateTime referenciaUtc,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AsNoTracking().FirstOrDefaultAsync(
            comissao => comissao.ProfissionalEstabelecimentoId == profissionalEstabelecimentoId
                && comissao.Ativo
                && comissao.InicioVigencia <= referenciaUtc
                && (comissao.FimVigencia == null || comissao.FimVigencia >= referenciaUtc),
            cancellationToken);
    }

    public Task<ComissaoProfissional?> ObterPorIdComTrackingAsync(
        int comissaoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            comissao => comissao.Id == comissaoId,
            cancellationToken);
    }
}
