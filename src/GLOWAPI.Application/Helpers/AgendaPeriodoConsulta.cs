namespace GLOWAPI.Application.Helpers;

public static class AgendaPeriodoConsulta
{
    public static (DateTime Inicio, DateTime Fim) ResolverIntervaloMesAtualUtc(DateTime? inicio, DateTime? fim)
    {
        if (inicio.HasValue && fim.HasValue)
        {
            return (inicio.Value, fim.Value);
        }

        var agora = DateTime.UtcNow;
        var inicioMes = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimMes = inicioMes.AddMonths(1);
        return (inicio ?? inicioMes, fim ?? fimMes);
    }

    public static (int Pagina, int TamanhoPagina) ResolverPaginacao(int pagina, int tamanhoPagina) =>
        (Math.Max(1, pagina), Math.Clamp(tamanhoPagina, 1, 50));
}
