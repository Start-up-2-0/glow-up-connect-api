using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Infrastructure.Repositories;

public class MetaProfissionalRepository : Repository<MetaProfissional>, IMetaProfissionalRepository
{
    public MetaProfissionalRepository(ApplicationDbContext context) : base(context)
    {
    }
}
