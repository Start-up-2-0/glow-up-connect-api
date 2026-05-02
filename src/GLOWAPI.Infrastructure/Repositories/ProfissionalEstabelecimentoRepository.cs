using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Infrastructure.Repositories;

public class ProfissionalEstabelecimentoRepository : Repository<ProfissionalEstabelecimento>, IProfissionalEstabelecimentoRepository
{
    public ProfissionalEstabelecimentoRepository(ApplicationDbContext context) : base(context)
    {
    }
}
