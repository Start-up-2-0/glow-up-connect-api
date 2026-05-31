using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class TelefoneHelperTests
{
    [Fact]
    public void CriarLinkWaMe_DeveGerarUrlComTextoEscapado()
    {
        var link = TelefoneHelper.CriarLinkWaMe("5511999999999", "GLOW 482913");

        Assert.Equal("https://wa.me/5511999999999?text=GLOW%20482913", link);
    }
}
