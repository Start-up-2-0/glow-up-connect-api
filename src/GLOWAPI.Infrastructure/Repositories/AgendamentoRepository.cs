using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Infrastructure.Repositories;

public class AgendamentoRepository : Repository<Agendamento>, IAgendamentoRepository
{
    public AgendamentoRepository(ApplicationDbContext context) : base(context)
    {
    }
}
