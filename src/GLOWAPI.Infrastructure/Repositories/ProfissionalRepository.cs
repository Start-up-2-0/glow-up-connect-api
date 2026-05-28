using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class ProfissionalRepository : Repository<Profissional>, IProfissionalRepository
{
    public ProfissionalRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Profissional?> ObterPorPublicGuidAsync(Guid publicGuid, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(profissional => profissional.PublicGuid == publicGuid, cancellationToken);
    }

    public Task<Profissional?> ObterPorUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(profissional => profissional.UsuarioId == usuarioId, cancellationToken);
    }

    public Task<Profissional?> ObterPorIdComEnderecoAsync(int id, CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(profissional => profissional.Endereco)
            .FirstOrDefaultAsync(profissional => profissional.Id == id, cancellationToken);
    }
}
