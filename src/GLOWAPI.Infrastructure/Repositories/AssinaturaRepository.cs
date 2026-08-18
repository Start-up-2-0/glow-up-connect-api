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
        AssinaturaStatus.Trial,
        AssinaturaStatus.Inadimplente,
        AssinaturaStatus.CancelamentoAgendado
    ];

    private static readonly AssinaturaStatus[] StatusAssinaturaComAcesso =
    [
        AssinaturaStatus.Ativa,
        AssinaturaStatus.Trial,
        AssinaturaStatus.Inadimplente,
        AssinaturaStatus.CancelamentoAgendado
    ];

    private static readonly AssinaturaStatus[] StatusAssinaturaCicloRegular =
    [
        AssinaturaStatus.Ativa,
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

    public async Task<Assinatura?> ObterAssinaturaEfetivaPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var direta = await ObterAtualPorEstabelecimentoAsync(estabelecimentoId, cancellationToken);
        if (direta?.Status is not null && StatusAssinaturaComAcesso.Contains(direta.Status))
        {
            return direta;
        }

        var vinculo = await Context.Set<AssinaturaEstabelecimento>()
            .Include(v => v.Assinatura)
                .ThenInclude(assinatura => assinatura!.Plano)
            .FirstOrDefaultAsync(v => v.EstabelecimentoId == estabelecimentoId, cancellationToken);

        if (vinculo?.Assinatura?.Status is not null
            && StatusAssinaturaComAcesso.Contains(vinculo.Assinatura.Status))
        {
            return vinculo.Assinatura;
        }

        return direta;
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

    public Task<int> ContarPorCodigoCampanhaAsync(
        string codigoCampanha,
        CancellationToken cancellationToken = default) =>
        DbSet.CountAsync(
            assinatura => assinatura.CampanhaPromocionalId != null
                && assinatura.CampanhaPromocional!.Codigo == codigoCampanha,
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
                StatusAssinaturaCicloRegular.Contains(assinatura.Status)
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
                StatusAssinaturaCicloRegular.Contains(assinatura.Status)
                && assinatura.ProximaDataGeracaoCobranca.HasValue
                && assinatura.ProximaDataGeracaoCobranca.Value.Date == data)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Assinatura>> ListarCancelamentosAgendadosParaEncerrarAsync(
        DateTime dataReferenciaUtc,
        CancellationToken cancellationToken = default)
    {
        var data = dataReferenciaUtc.Date;
        return await DbSet
            .Include(assinatura => assinatura.Plano)
            .Where(assinatura =>
                assinatura.Status == AssinaturaStatus.CancelamentoAgendado
                && assinatura.Fim.HasValue
                && assinatura.Fim.Value.Date <= data)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Assinatura>> ListarPendentesComOnboardingJsonAsync(
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(assinatura => assinatura.Plano)
            .Where(assinatura =>
                assinatura.Status == AssinaturaStatus.PendentePagamento
                && assinatura.OnboardingPendenteJson != null
                && assinatura.OnboardingPendenteJson != string.Empty)
            .OrderByDescending(assinatura => assinatura.Id)
            .ToListAsync(cancellationToken);

    public async Task<bool> UsuarioJaTeveAssinaturaAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var estabelecimentoIds = await Context.Set<EstabelecimentoUsuario>()
            .Where(vinculo =>
                vinculo.UsuarioId == usuarioId
                && vinculo.Ativo
                && vinculo.RoleNoEstabelecimento == EstablishmentUserRole.Owner)
            .Select(vinculo => vinculo.EstabelecimentoId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (estabelecimentoIds.Count == 0)
        {
            return false;
        }

        var possuiDireta = await DbSet.AnyAsync(
            assinatura => assinatura.EstabelecimentoId.HasValue
                && estabelecimentoIds.Contains(assinatura.EstabelecimentoId.Value),
            cancellationToken);

        if (possuiDireta)
        {
            return true;
        }

        return await Context.Set<AssinaturaEstabelecimento>()
            .AnyAsync(
                vinculo => estabelecimentoIds.Contains(vinculo.EstabelecimentoId),
                cancellationToken);
    }

    public async Task<bool> UsuarioPossuiAssinaturaComAcessoAsync(
        int usuarioId,
        int? ignorarAssinaturaId = null,
        CancellationToken cancellationToken = default)
    {
        var estabelecimentoIds = await Context.Set<EstabelecimentoUsuario>()
            .Where(vinculo =>
                vinculo.UsuarioId == usuarioId
                && vinculo.Ativo
                && vinculo.RoleNoEstabelecimento == EstablishmentUserRole.Owner)
            .Select(vinculo => vinculo.EstabelecimentoId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var estabelecimentoId in estabelecimentoIds)
        {
            var assinatura = await ObterAssinaturaEfetivaPorEstabelecimentoAsync(estabelecimentoId, cancellationToken);
            if (assinatura is null)
            {
                continue;
            }

            if (ignorarAssinaturaId.HasValue && assinatura.Id == ignorarAssinaturaId.Value)
            {
                continue;
            }

            if (StatusAssinaturaComAcesso.Contains(assinatura.Status))
            {
                return true;
            }
        }

        return false;
    }

}
