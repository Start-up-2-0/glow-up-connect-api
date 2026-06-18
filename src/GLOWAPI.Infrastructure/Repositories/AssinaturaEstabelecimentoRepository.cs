using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class AssinaturaEstabelecimentoRepository : Repository<AssinaturaEstabelecimento>, IAssinaturaEstabelecimentoRepository
{
    public AssinaturaEstabelecimentoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<AssinaturaEstabelecimento?> ObterPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(vinculo => vinculo.Assinatura)
                .ThenInclude(assinatura => assinatura!.Plano)
            .FirstOrDefaultAsync(vinculo => vinculo.EstabelecimentoId == estabelecimentoId, cancellationToken);
    }

    public Task<int> ContarPorAssinaturaAsync(
        int assinaturaId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.CountAsync(vinculo => vinculo.AssinaturaId == assinaturaId, cancellationToken);
    }

    public Task<AssinaturaEstabelecimento?> ObterMatrizPorAssinaturaAsync(
        int assinaturaId,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(vinculo => vinculo.Estabelecimento)
            .FirstOrDefaultAsync(
                vinculo => vinculo.AssinaturaId == assinaturaId && vinculo.EhMatriz,
                cancellationToken);
    }

    public async Task<IReadOnlyList<AssinaturaEstabelecimento>> ListarPorAssinaturaAsync(
        int assinaturaId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(vinculo => vinculo.Estabelecimento)
            .Where(vinculo => vinculo.AssinaturaId == assinaturaId)
            .OrderByDescending(vinculo => vinculo.EhMatriz)
            .ThenBy(vinculo => vinculo.CreateAd)
            .ToListAsync(cancellationToken);
    }
}
