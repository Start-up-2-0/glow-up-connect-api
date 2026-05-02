using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Infrastructure.Repositories;

public class HorarioAtendimentoProfissionalRepository : Repository<HorarioAtendimentoProfissional>, IHorarioAtendimentoProfissionalRepository
{
    public HorarioAtendimentoProfissionalRepository(ApplicationDbContext context) : base(context)
    {
    }
}
