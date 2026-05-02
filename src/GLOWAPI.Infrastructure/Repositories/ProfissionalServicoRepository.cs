using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Infrastructure.Repositories;

public class ProfissionalServicoRepository : Repository<ProfissionalServico>, IProfissionalServicoRepository
{
    public ProfissionalServicoRepository(ApplicationDbContext context) : base(context)
    {
    }
}
