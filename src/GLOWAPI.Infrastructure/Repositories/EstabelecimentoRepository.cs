using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class EstabelecimentoRepository : Repository<Estabelecimento>, IEstabelecimentoRepository
{
    public EstabelecimentoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Estabelecimento?> ObterPorPublicGuidAsync(Guid publicGuid, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(estabelecimento => estabelecimento.PublicGuid == publicGuid, cancellationToken);
    }
}
