using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class FavoritoClienteRepository : Repository<FavoritoCliente>, IFavoritoClienteRepository
{
    public FavoritoClienteRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<FavoritoCliente>> ListarPorUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(favorito => favorito.Estabelecimento)
            .Include(favorito => favorito.Profissional)
            .Where(favorito => favorito.UsuarioClienteId == usuarioId)
            .OrderByDescending(favorito => favorito.CriadoEm)
            .ToListAsync(cancellationToken);

    public Task<FavoritoCliente?> ObterAsync(int usuarioId, int estabelecimentoId, int? profissionalId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(favorito => favorito.Estabelecimento)
            .Include(favorito => favorito.Profissional)
            .FirstOrDefaultAsync(favorito =>
                favorito.UsuarioClienteId == usuarioId
                && favorito.EstabelecimentoId == estabelecimentoId
                && favorito.ProfissionalId == profissionalId,
                cancellationToken);

    public Task<FavoritoCliente?> ObterPorIdAsync(int id, int usuarioId, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(favorito => favorito.Id == id && favorito.UsuarioClienteId == usuarioId, cancellationToken);
}
