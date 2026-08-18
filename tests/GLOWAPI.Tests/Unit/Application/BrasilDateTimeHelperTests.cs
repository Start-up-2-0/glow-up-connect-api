using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class BrasilDateTimeHelperTests
{
    [Fact]
    public void Agora_DeveFicarAtrasDeUtcEmTresHoras()
    {
        var utc = DateTime.UtcNow;
        var brasil = BrasilDateTimeHelper.Agora();
        var diferenca = utc - brasil;

        Assert.True(diferenca.TotalHours is >= 2.9 and <= 3.1);
    }

    [Fact]
    public void FormatarIsoComOffset_DeveUsarMenosTres()
    {
        var horario = new DateTime(2026, 8, 18, 12, 30, 0, DateTimeKind.Unspecified);
        var iso = BrasilDateTimeHelper.FormatarIsoComOffset(horario);

        Assert.StartsWith("2026-08-18T12:30:00", iso);
        Assert.Contains("-03:00", iso, StringComparison.Ordinal);
    }
}
