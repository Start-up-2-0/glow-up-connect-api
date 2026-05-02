using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Infrastructure.Repositories;

public class CaixaRepository : Repository<Caixa>, ICaixaRepository
{
    public CaixaRepository(ApplicationDbContext context) : base(context)
    {
    }
}
