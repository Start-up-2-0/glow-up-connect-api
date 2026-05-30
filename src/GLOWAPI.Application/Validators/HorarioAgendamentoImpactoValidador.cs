using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Validators;

public static class HorarioAgendamentoImpactoValidador
{
    public static async Task ValidarAlteracaoAsync(
        IAgendamentoItemRepository agendamentoItemRepository,
        int estabelecimentoId,
        int profissionalId,
        DayOfWeek diaSemanaAtual,
        TimeOnly horaInicioAtual,
        TimeOnly horaFimAtual,
        DayOfWeek novoDiaSemana,
        TimeOnly novaHoraInicio,
        TimeOnly novaHoraFim,
        CancellationToken cancellationToken)
    {
        var impactados = await agendamentoItemRepository.ListarAgendamentosFuturosImpactadosPorAlteracaoHorarioAsync(
            estabelecimentoId,
            profissionalId,
            diaSemanaAtual,
            horaInicioAtual,
            horaFimAtual,
            novoDiaSemana,
            novaHoraInicio,
            novaHoraFim,
            cancellationToken);

        LancarSeHouverImpacto(impactados, "A alteracao impactaria agendamentos futuros do profissional.");
    }

    public static async Task ValidarInativacaoAsync(
        IAgendamentoItemRepository agendamentoItemRepository,
        int estabelecimentoId,
        int profissionalId,
        DayOfWeek diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        CancellationToken cancellationToken)
    {
        var impactados = await agendamentoItemRepository.ListarAgendamentosFuturosNoHorarioAtivoAsync(
            estabelecimentoId,
            profissionalId,
            diaSemana,
            horaInicio,
            horaFim,
            cancellationToken);

        LancarSeHouverImpacto(impactados, "A inativacao impactaria agendamentos futuros do profissional.");
    }

    private static void LancarSeHouverImpacto(
        IReadOnlyList<AgendamentoFuturoImpactadoDto> impactados,
        string mensagem)
    {
        if (impactados.Count == 0)
        {
            return;
        }

        throw new HorarioAlteracaoImpactaAgendamentosFuturosException(
            mensagem,
            impactados
                .Select(item => new AgendamentoFuturoImpactadoResumo(
                    item.AgendamentoItemId,
                    item.AgendamentoId,
                    item.ProfissionalId,
                    item.Inicio,
                    item.Fim))
                .ToList());
    }
}
