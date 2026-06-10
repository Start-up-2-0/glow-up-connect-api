using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Helpers;

public static class AgendamentoHorarioHelper
{
    public static DateTime ObterInicio(Agendamento agendamento)
    {
        if (agendamento.Inicio != default)
        {
            return agendamento.Inicio;
        }

        return agendamento.Itens
            .OrderBy(item => item.Inicio)
            .FirstOrDefault()
            ?.Inicio
            ?? default;
    }

    public static DateTime ObterFim(Agendamento agendamento)
    {
        if (agendamento.Fim != default)
        {
            return agendamento.Fim;
        }

        return agendamento.Itens
            .OrderBy(item => item.Inicio)
            .LastOrDefault()
            ?.Fim
            ?? default;
    }

    public static int ObterDuracaoTotalMinutos(Agendamento agendamento)
    {
        var inicio = ObterInicio(agendamento);
        var fim = ObterFim(agendamento);

        if (inicio == default || fim == default || fim <= inicio)
        {
            return agendamento.Itens.Sum(item => (int)(item.Fim - item.Inicio).TotalMinutes);
        }

        return (int)(fim - inicio).TotalMinutes;
    }
}
