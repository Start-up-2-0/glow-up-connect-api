using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.Email == email, cancellationToken);
    }

    public Task<Usuario?> ObterPorConfirmacaoTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.ConfirmacaoTokenHash == tokenHash, cancellationToken);
    }

    public Task<Usuario?> ObterPorConfirmacaoCodigoHashAsync(string codigoHash, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.ConfirmacaoCodigoHash == codigoHash, cancellationToken);
    }
}
