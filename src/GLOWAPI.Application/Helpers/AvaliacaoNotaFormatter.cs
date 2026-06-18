namespace GLOWAPI.Application.Helpers;

public static class AvaliacaoNotaFormatter
{
    public const int JanelaDias = 90;

    public static DateTime ObterInicioJanela(DateTime? referenciaUtc = null) =>
        (referenciaUtc ?? DateTime.UtcNow).AddDays(-JanelaDias);

    public static bool NotaValida(int nota) => nota is >= 0 and <= 5;

    public static decimal CalcularMediaBruta(IReadOnlyList<byte> notas)
    {
        if (notas.Count == 0)
        {
            return 0m;
        }

        return (decimal)notas.Sum(nota => nota) / notas.Count;
    }

    public static decimal FormatarMediaIfood(decimal mediaBruta)
    {
        if (mediaBruta <= 0m)
        {
            return 0m;
        }

        var centesimo = (int)(mediaBruta * 100m) % 10;
        var decimos = Math.Floor(mediaBruta * 10m);

        return centesimo <= 5
            ? decimos / 10m
            : Math.Ceiling(mediaBruta * 10m) / 10m;
    }

    public static IReadOnlyDictionary<int, int> CalcularDistribuicao(IReadOnlyList<byte> notas)
    {
        var distribuicao = Enumerable.Range(0, 6).ToDictionary(nota => nota, _ => 0);

        foreach (var nota in notas)
        {
            if (distribuicao.ContainsKey(nota))
            {
                distribuicao[nota]++;
            }
        }

        return distribuicao;
    }
}
