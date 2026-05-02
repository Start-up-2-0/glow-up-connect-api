using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Infrastructure.Repositories;

public class HorarioFuncionamentoEstabelecimentoRepository : Repository<HorarioFuncionamentoEstabelecimento>, IHorarioFuncionamentoEstabelecimentoRepository
{
    public HorarioFuncionamentoEstabelecimentoRepository(ApplicationDbContext context) : base(context)
    {
    }
}
