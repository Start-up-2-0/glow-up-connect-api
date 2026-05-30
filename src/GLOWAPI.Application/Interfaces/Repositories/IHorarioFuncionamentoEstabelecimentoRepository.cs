using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IHorarioFuncionamentoEstabelecimentoRepository : IRepository<HorarioFuncionamentoEstabelecimento>
{
    Task<HorarioFuncionamentoEstabelecimento?> ObterPorIdEEstabelecimentoAsync(
        int horarioId,
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HorarioFuncionamentoEstabelecimento>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        DayOfWeek? diaSemana = null,
        bool? ativo = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HorarioFuncionamentoEstabelecimento>> ListarAtivosPorEstabelecimentoEDiaAsync(
        int estabelecimentoId,
        DayOfWeek diaSemana,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteConflitoAtivoAsync(
        int estabelecimentoId,
        DayOfWeek diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        int? ignorarHorarioId = null,
        CancellationToken cancellationToken = default);
}
