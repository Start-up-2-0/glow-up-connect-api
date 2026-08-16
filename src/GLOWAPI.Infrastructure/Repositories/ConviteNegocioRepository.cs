using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class ConviteNegocioRepository : Repository<ConviteNegocio>, IConviteNegocioRepository
{
    public ConviteNegocioRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<ConviteNegocio?> ObterPorTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(convite => convite.Estabelecimento)
            .FirstOrDefaultAsync(convite => convite.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<ConviteNegocio>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        StatusConviteNegocio? status,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(convite => convite.EstabelecimentoId == estabelecimentoId);

        if (status.HasValue)
        {
            query = query.Where(convite => convite.Status == status.Value);
        }

        return await query
            .OrderByDescending(convite => convite.CriadoEm)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> UsuarioJaUtilizouAsync(
        int conviteId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        return Context.ConvitesNegocioUtilizacoes.AnyAsync(
            utilizacao => utilizacao.ConviteNegocioId == conviteId
                && utilizacao.UsuarioId == usuarioId,
            cancellationToken);
    }

    public async Task AdicionarUtilizacaoAsync(
        ConviteNegocioUtilizacao utilizacao,
        CancellationToken cancellationToken = default)
    {
        await Context.ConvitesNegocioUtilizacoes.AddAsync(utilizacao, cancellationToken);
    }

    public async Task<bool> TentarRegistrarUtilizacaoAsync(
        int conviteId,
        DateTime agoraUtc,
        CancellationToken cancellationToken = default)
    {
        var afetados = await Context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE `ConvitesNegocio`
             SET `QuantidadeUtilizacoes` = `QuantidadeUtilizacoes` + 1,
                 `Status` = CASE
                     WHEN `QuantidadeUtilizacoes` + 1 >= `LimiteUsuarios` THEN 'Esgotado'
                     ELSE `Status`
                 END,
                 `UpdatedAt` = {agoraUtc}
             WHERE `Id` = {conviteId}
               AND `Status` = 'Ativo'
               AND `ExpiraEm` > {agoraUtc}
               AND `QuantidadeUtilizacoes` < `LimiteUsuarios`
             """,
            cancellationToken);

        return afetados > 0;
    }

    public async Task CompensarUtilizacaoAsync(
        int conviteId,
        DateTime agoraUtc,
        CancellationToken cancellationToken = default)
    {
        await Context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE `ConvitesNegocio`
             SET `QuantidadeUtilizacoes` = GREATEST(`QuantidadeUtilizacoes` - 1, 0),
                 `Status` = CASE
                     WHEN `Status` = 'Esgotado'
                          AND `QuantidadeUtilizacoes` - 1 < `LimiteUsuarios`
                          AND `ExpiraEm` > {agoraUtc}
                     THEN 'Ativo'
                     ELSE `Status`
                 END,
                 `UpdatedAt` = {agoraUtc}
             WHERE `Id` = {conviteId}
             """,
            cancellationToken);
    }
}
