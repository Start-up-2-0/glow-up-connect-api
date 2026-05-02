using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Infrastructure.Repositories;

public class ComissaoProfissionalRepository : Repository<ComissaoProfissional>, IComissaoProfissionalRepository
{
    public ComissaoProfissionalRepository(ApplicationDbContext context) : base(context)
    {
    }
}
