using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Helpers;

public static class HorarioFuncionamentoPublicoHelper
{
    private static readonly TimeZoneInfo FusoBrasil = ObterFusoBrasil();

    public static (bool AbertoAgora, string? HorarioAbertura, string? HorarioFechamento) ResolverParaHoje(
        IReadOnlyList<HorarioFuncionamentoEstabelecimento> horarios)
    {
        var horariosDoDia = horarios
            .Where(horario => horario.Ativo)
            .Where(horario => horario.DiaSemana == ObterDiaSemanaAtualBrasil())
            .OrderBy(horario => horario.HoraInicio)
            .ToList();

        if (horariosDoDia.Count == 0)
        {
            return (false, null, null);
        }

        var abertura = horariosDoDia[0].HoraInicio;
        var fechamento = horariosDoDia[^1].HoraFim;
        var horaAtual = TimeOnly.FromDateTime(ObterDataHoraAtualBrasil());
        var abertoAgora = horariosDoDia.Any(horario =>
            horaAtual >= horario.HoraInicio && horaAtual < horario.HoraFim);

        return (abertoAgora, abertura.ToString("HH:mm"), fechamento.ToString("HH:mm"));
    }

    private static DateTime ObterDataHoraAtualBrasil() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FusoBrasil);

    private static DayOfWeek ObterDiaSemanaAtualBrasil() =>
        ObterDataHoraAtualBrasil().DayOfWeek;

    private static TimeZoneInfo ObterFusoBrasil()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }
}
