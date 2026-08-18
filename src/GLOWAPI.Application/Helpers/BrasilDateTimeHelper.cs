using System.Globalization;

namespace GLOWAPI.Application.Helpers;

/// <summary>
/// Relógio operacional do Glow em horário de Brasília (America/Sao_Paulo, UTC-3).
/// Use para persistir e comparar datas que devem aparecer no banco sem conversão.
/// </summary>
public static class BrasilDateTimeHelper
{
    private static readonly TimeZoneInfo FusoBrasil = ObterFusoBrasil();

    public static DateTime Agora() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FusoBrasil);

    public static DateTimeOffset ParaOffset(DateTime horarioBrasil)
    {
        var wallClock = DateTime.SpecifyKind(horarioBrasil, DateTimeKind.Unspecified);
        var offset = FusoBrasil.GetUtcOffset(DateTime.UtcNow);
        return new DateTimeOffset(wallClock, offset);
    }

    public static string FormatarIsoComOffset(DateTime horarioBrasil) =>
        ParaOffset(horarioBrasil).ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);

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
