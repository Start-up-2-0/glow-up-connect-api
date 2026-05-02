using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Infrastructure.Repositories;

public class ServicoRepository : Repository<Servico>, IServicoRepository
{
    public ServicoRepository(ApplicationDbContext context) : base(context)
    {
    }
}
