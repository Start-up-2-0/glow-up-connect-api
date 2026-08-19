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
            .ThenInclude(estabelecimento => estabelecimento!.CategoriaEstabelecimento)
            .Where(vinculo => vinculo.UsuarioId == usuarioId
                && vinculo.Ativo
                && vinculo.Estabelecimento != null
                && vinculo.Estabelecimento.Ativo)
            .OrderBy(vinculo => vinculo.Estabelecimento!.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<EstabelecimentoUsuario?> ObterOwnerAtivoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(vinculo => vinculo.Usuario)
            .FirstOrDefaultAsync(
                vinculo => vinculo.EstabelecimentoId == estabelecimentoId
                    && vinculo.Ativo
                    && vinculo.RoleNoEstabelecimento == EstablishmentUserRole.Owner,
                cancellationToken);
    }

    public async Task<IReadOnlyList<EstabelecimentoUsuario>> ListarAtivosPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(vinculo => vinculo.EstabelecimentoId == estabelecimentoId && vinculo.Ativo)
            .OrderBy(vinculo => vinculo.Usuario!.Nome)
            .Select(vinculo => new EstabelecimentoUsuario
            {
                Id = vinculo.Id,
                EstabelecimentoId = vinculo.EstabelecimentoId,
                UsuarioId = vinculo.UsuarioId,
                RoleNoEstabelecimento = vinculo.RoleNoEstabelecimento,
                Ativo = vinculo.Ativo,
                CreateAd = vinculo.CreateAd,
                UpdatedAt = vinculo.UpdatedAt,
                Usuario = vinculo.Usuario == null
                    ? null
                    : new Usuario
                    {
                        Id = vinculo.Usuario.Id,
                        Nome = vinculo.Usuario.Nome,
                        Email = vinculo.Usuario.Email,
                        Telefone = vinculo.Usuario.Telefone,
                        Role = vinculo.Usuario.Role,
                        Sexo = vinculo.Usuario.Sexo,
                        Ativo = vinculo.Usuario.Ativo,
                        AvatarBase64 = null,
                        CreatedAt = vinculo.Usuario.CreatedAt,
                        UpdatedAt = vinculo.Usuario.UpdatedAt,
                    },
            })
            .ToListAsync(cancellationToken);
    }
}
