using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class CaixaRepository : Repository<Caixa>, ICaixaRepository
{
    public CaixaRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Caixa?> ObterPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AsNoTracking().FirstOrDefaultAsync(
            caixa => caixa.EstabelecimentoId == estabelecimentoId,
            cancellationToken);
    }

    public Task<Caixa?> ObterPorEstabelecimentoComTrackingAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            caixa => caixa.EstabelecimentoId == estabelecimentoId,
            cancellationToken);
    }

    public async Task<Caixa> ObterOuProvisionarPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var caixa = await ObterPorEstabelecimentoAsync(estabelecimentoId, cancellationToken);
        if (caixa is not null)
        {
            return caixa;
        }

        return await ObterOuProvisionarPorEstabelecimentoComTrackingAsync(
            estabelecimentoId,
            cancellationToken);
    }

    public async Task<Caixa> ObterOuProvisionarPorEstabelecimentoComTrackingAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var caixa = await ObterPorEstabelecimentoComTrackingAsync(estabelecimentoId, cancellationToken);
        if (caixa is not null)
        {
            return caixa;
        }

        caixa = new Caixa
        {
            EstabelecimentoId = estabelecimentoId,
            CreateAd = DateTime.UtcNow
        };

        await AdicionarAsync(caixa, cancellationToken);
        await SalvarAlteracoesAsync(cancellationToken);
        return caixa;
    }
}
