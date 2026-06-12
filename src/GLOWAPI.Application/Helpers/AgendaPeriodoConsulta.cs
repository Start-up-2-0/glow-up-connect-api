using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Helpers;

public static class AgendaPeriodoConsulta
{
    public const int MaximoDiasIntervaloPersonalizado = 364;

    public static void ValidarIntervaloPersonalizado(DateTime inicio, DateTime fim, DateTime? referenciaUtc = null)
    {
        var inicioDate = ParaDataCalendario(inicio);
        var fimDate = ParaDataCalendario(fim);
        var hoje = ParaDataCalendario(referenciaUtc ?? DateTime.UtcNow);

        ValidarOrdemIntervalo(inicioDate, fimDate);

        if (fimDate > hoje)
        {
            throw new AgendaPeriodoConsultaInvalidoException("A data final nao pode ultrapassar a data atual.");
        }

        if (fimDate.DayNumber - inicioDate.DayNumber > MaximoDiasIntervaloPersonalizado)
        {
            throw new AgendaPeriodoConsultaInvalidoException("O intervalo nao pode ultrapassar 1 ano.");
        }
    }

    public static (DateTime Inicio, DateTime Fim) ResolverIntervaloMesAtualUtc(
        DateTime? inicio,
        DateTime? fim,
        bool intervaloPersonalizado = false)
    {
        if (inicio.HasValue && fim.HasValue)
        {
            if (intervaloPersonalizado)
            {
                ValidarIntervaloPersonalizado(inicio.Value, fim.Value);
            }
            else
            {
                ValidarOrdemIntervalo(ParaDataCalendario(inicio.Value), ParaDataCalendario(fim.Value));
            }

            return (inicio.Value, fim.Value);
        }

        var agora = DateTime.UtcNow;
        var inicioMes = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimMes = inicioMes.AddMonths(1);
        return (inicio ?? inicioMes, fim ?? fimMes);
    }

    public static (int Pagina, int TamanhoPagina) ResolverPaginacao(int pagina, int tamanhoPagina) =>
        (Math.Max(1, pagina), Math.Clamp(tamanhoPagina, 1, 50));

    private static void ValidarOrdemIntervalo(DateOnly inicioDate, DateOnly fimDate)
    {
        if (fimDate < inicioDate)
        {
            throw new AgendaPeriodoConsultaInvalidoException("A data final nao pode ser anterior a data inicial.");
        }
    }

    private static DateOnly ParaDataCalendario(DateTime value) =>
        new(value.Year, value.Month, value.Day);
}
