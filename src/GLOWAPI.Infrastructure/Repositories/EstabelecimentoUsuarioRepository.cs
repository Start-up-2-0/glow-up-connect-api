using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class EstabelecimentoUsuarioRepository : Repository<EstabelecimentoUsuario>, IEstabelecimentoUsuarioRepository
{
    public EstabelecimentoUsuarioRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<EstabelecimentoUsuario?> ObterAtivoAsync(
        int estabelecimentoId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            estabelecimentoUsuario => estabelecimentoUsuario.EstabelecimentoId == estabelecimentoId
                && estabelecimentoUsuario.UsuarioId == usuarioId
                && estabelecimentoUsuario.Ativo,
            cancellationToken);
    }

    public Task<EstabelecimentoUsuario?> ObterPorUsuarioAsync(
        int estabelecimentoId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            estabelecimentoUsuario => estabelecimentoUsuario.EstabelecimentoId == estabelecimentoId
                && estabelecimentoUsuario.UsuarioId == usuarioId,
            cancellationToken);
    }

    public Task<int> ContarAtivosAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.CountAsync(
            estabelecimentoUsuario => estabelecimentoUsuario.EstabelecimentoId == estabelecimentoId
                && estabelecimentoUsuario.Ativo,
            cancellationToken);
    }

    public Task<int> ContarOwnersAtivosAsync(
        int estabelecimentoId,
        int? ignorarUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        return DbSet.CountAsync(
            estabelecimentoUsuario => estabelecimentoUsuario.EstabelecimentoId == estabelecimentoId
                && estabelecimentoUsuario.Ativo
                && estabelecimentoUsuario.RoleNoEstabelecimento == EstablishmentUserRole.Owner
                && (!ignorarUsuarioId.HasValue || estabelecimentoUsuario.UsuarioId != ignorarUsuarioId.Value),
            cancellationToken);
    }

    public async Task<IReadOnlyList<EstabelecimentoUsuario>> ListarAtivosPorUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(vinculo => vinculo.Estabelecimento)
            .Where(vinculo => vinculo.UsuarioId == usuarioId
                && vinculo.Ativo
                && vinculo.Estabelecimento != null
                && vinculo.Estabelecimento.Ativo)
            .OrderBy(vinculo => vinculo.Estabelecimento!.Nome)
            .ToListAsync(cancellationToken);
    }
}
