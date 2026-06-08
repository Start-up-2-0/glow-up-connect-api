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

    public Task<Assinatura?> ObterPorIdComPlanoAsync(int assinaturaId, CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(assinatura => assinatura.Plano)
            .Include(assinatura => assinatura.PlanoAlteracaoPendente)
            .FirstOrDefaultAsync(assinatura => assinatura.Id == assinaturaId, cancellationToken);
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

    public Task<bool> ExisteAtivaOuPendentePorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            assinatura => assinatura.EstabelecimentoId == estabelecimentoId
                && StatusBloqueadosParaNovaAssinatura.Contains(assinatura.Status),
            cancellationToken);
    }

    public Task<bool> ExisteComCampanhaPorEstabelecimentoAsync(
        int estabelecimentoId,
        string codigoCampanha,
        CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(
            assinatura => assinatura.EstabelecimentoId == estabelecimentoId
                && assinatura.CampanhaPromocional != null
                && assinatura.CampanhaPromocional.Codigo == codigoCampanha,
            cancellationToken);

    public async Task<IReadOnlyList<Assinatura>> ListarParaAlertaFaturaAsync(
        DateTime dataReferenciaUtc,
        CancellationToken cancellationToken = default)
    {
        var data = dataReferenciaUtc.Date;
        return await DbSet
            .Include(assinatura => assinatura.Plano)
            .Include(assinatura => assinatura.Estabelecimento)
            .Where(assinatura =>
                (assinatura.Status == AssinaturaStatus.Ativa || assinatura.Status == AssinaturaStatus.Trial)
                && assinatura.ProximaDataAlerta.HasValue
                && assinatura.ProximaDataAlerta.Value.Date == data
                && (assinatura.UltimoAlertaFaturaEm == null
                    || assinatura.UltimoAlertaFaturaEm.Value.Date < data))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Assinatura>> ListarParaGeracaoCobrancaAsync(
        DateTime dataReferenciaUtc,
        CancellationToken cancellationToken = default)
    {
        var data = dataReferenciaUtc.Date;
        return await DbSet
            .Include(assinatura => assinatura.Plano)
            .Include(assinatura => assinatura.Estabelecimento)
            .Where(assinatura =>
                (assinatura.Status == AssinaturaStatus.Ativa || assinatura.Status == AssinaturaStatus.Trial)
                && assinatura.ProximaDataGeracaoCobranca.HasValue
                && assinatura.ProximaDataGeracaoCobranca.Value.Date == data)
            .ToListAsync(cancellationToken);
    }

}
