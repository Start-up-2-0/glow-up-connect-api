using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Infrastructure.Repositories;

public class LancamentoCaixaRepository : Repository<LancamentoCaixa>, ILancamentoCaixaRepository
{
    public LancamentoCaixaRepository(ApplicationDbContext context) : base(context)
    {
    }
}
