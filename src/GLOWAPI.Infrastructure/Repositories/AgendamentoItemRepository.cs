using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Infrastructure.Repositories;

public class AgendamentoItemRepository : Repository<AgendamentoItem>, IAgendamentoItemRepository
{
    public AgendamentoItemRepository(ApplicationDbContext context) : base(context)
    {
    }
}
