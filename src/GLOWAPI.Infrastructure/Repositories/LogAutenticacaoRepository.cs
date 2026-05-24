using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Infrastructure.Repositories;

public class LogAutenticacaoRepository : Repository<LogAutenticacao>, ILogAutenticacaoRepository
{
    public LogAutenticacaoRepository(ApplicationDbContext context) : base(context)
    {
    }
}
