using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class SessaoAutenticacaoRepository : Repository<SessaoAutenticacao>, ISessaoAutenticacaoRepository
{
    public SessaoAutenticacaoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<SessaoAutenticacao?> ObterAtivaPorRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;

        return DbSet
            .Include(s => s.Usuario)
            .FirstOrDefaultAsync(
                s => s.RefreshTokenHash == refreshTokenHash
                     && s.RevogadoEm == null
                     && s.ExpiraEm > agora,
                cancellationToken);
    }

    public Task<SessaoAutenticacao?> ObterAtivaPorAccessTokenHashAsync(
        string accessTokenHash,
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;

        return DbSet
            .Include(s => s.Usuario)
            .FirstOrDefaultAsync(
                s => s.Id == sessionId
                     && s.AccessTokenHash == accessTokenHash
                     && s.RevogadoEm == null
                     && s.ExpiraEm > agora,
                cancellationToken);
    }

    public async Task<IReadOnlyList<SessaoAutenticacao>> ListarAtivasPorUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;

        return await DbSet
            .Where(s => s.UsuarioId == usuarioId
                        && s.RevogadoEm == null
                        && s.ExpiraEm > agora)
            .ToListAsync(cancellationToken);
    }
}
