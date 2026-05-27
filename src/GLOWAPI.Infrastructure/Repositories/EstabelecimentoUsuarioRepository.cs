using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
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
}
