namespace GLOWAPI.Application.Helpers;

internal static class GeradorSlotsDisponibilidade
{
    public static IEnumerable<(DateTime Inicio, DateTime Fim)> Gerar(
        DateOnly data,
        TimeOnly janelaInicio,
        TimeOnly janelaFim,
        int duracaoMinutos,
        int intervaloMinutos = 15)
    {
        var cursor = data.ToDateTime(janelaInicio, DateTimeKind.Utc);
        var limite = data.ToDateTime(janelaFim, DateTimeKind.Utc);

        while (cursor.AddMinutes(duracaoMinutos) <= limite)
        {
            var fim = cursor.AddMinutes(duracaoMinutos);
            yield return (cursor, fim);
            cursor = cursor.AddMinutes(intervaloMinutos);
        }
    }

    public static (TimeOnly Inicio, TimeOnly Fim)? Intersectar(
        TimeOnly inicioA,
        TimeOnly fimA,
        TimeOnly inicioB,
        TimeOnly fimB)
    {
        var inicio = inicioA > inicioB ? inicioA : inicioB;
        var fim = fimA < fimB ? fimA : fimB;

        return inicio < fim ? (inicio, fim) : null;
    }
}
