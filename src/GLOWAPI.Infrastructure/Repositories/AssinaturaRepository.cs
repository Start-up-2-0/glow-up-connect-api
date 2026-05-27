using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class AssinaturaRepository : Repository<Assinatura>, IAssinaturaRepository
{
    private static readonly AssinaturaStatus[] StatusBloqueadosParaNovaAssinatura =
    [
        AssinaturaStatus.Ativa,
        AssinaturaStatus.PendentePagamento,
        AssinaturaStatus.Trial
    ];

    public AssinaturaRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Assinatura?> ObterAtivaPorEstabelecimentoAsync(int estabelecimentoId, CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(assinatura => assinatura.Plano)
            .FirstOrDefaultAsync(
                assinatura => assinatura.EstabelecimentoId == estabelecimentoId
                    && assinatura.Status == AssinaturaStatus.Ativa,
                cancellationToken);
    }

    public Task<Assinatura?> ObterAtivaPorProfissionalAutonomoAsync(int profissionalId, CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(assinatura => assinatura.Plano)
            .FirstOrDefaultAsync(
                assinatura => assinatura.ProfissionalAutonomoId == profissionalId
                    && assinatura.Status == AssinaturaStatus.Ativa,
                cancellationToken);
    }

    public Task<Assinatura?> ObterAtualPorEstabelecimentoAsync(int estabelecimentoId, CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(assinatura => assinatura.Plano)
            .Where(assinatura => assinatura.EstabelecimentoId == estabelecimentoId)
            .OrderByDescending(assinatura => assinatura.CreateAd)
            .ThenByDescending(assinatura => assinatura.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Assinatura?> ObterAtualPorProfissionalAutonomoAsync(int profissionalId, CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(assinatura => assinatura.Plano)
            .Where(assinatura => assinatura.ProfissionalAutonomoId == profissionalId)
            .OrderByDescending(assinatura => assinatura.CreateAd)
            .ThenByDescending(assinatura => assinatura.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExisteAtivaOuPendentePorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            assinatura => assinatura.EstabelecimentoId == estabelecimentoId
                && StatusBloqueadosParaNovaAssinatura.Contains(assinatura.Status),
            cancellationToken);
    }

    public Task<bool> ExisteAtivaOuPendentePorProfissionalAutonomoAsync(
        int profissionalId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            assinatura => assinatura.ProfissionalAutonomoId == profissionalId
                && StatusBloqueadosParaNovaAssinatura.Contains(assinatura.Status),
            cancellationToken);
    }
}
