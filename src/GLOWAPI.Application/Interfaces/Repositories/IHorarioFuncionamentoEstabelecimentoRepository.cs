using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IHorarioFuncionamentoEstabelecimentoRepository : IRepository<HorarioFuncionamentoEstabelecimento>
{
    Task<IReadOnlyList<HorarioFuncionamentoEstabelecimento>> ListarAtivosPorEstabelecimentoEDiaAsync(
        int estabelecimentoId,
        DayOfWeek diaSemana,
        CancellationToken cancellationToken = default);
}
