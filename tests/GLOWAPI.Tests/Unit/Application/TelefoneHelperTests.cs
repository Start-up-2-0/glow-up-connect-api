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

    [Fact]
    public void SaoEquivalentes_DeveEquivalerCelularBrComNonoDigito()
    {
        Assert.True(TelefoneHelper.SaoEquivalentes("5579991917634", "557991917634"));
        Assert.True(TelefoneHelper.SaoEquivalentes("79991917634", "7991917634"));
    }

    [Fact]
    public void SaoEquivalentes_NaoDeveEquivalerDddsDiferentes()
    {
        Assert.False(TelefoneHelper.SaoEquivalentes("5511988887777", "5521988887777"));
    }

    [Fact]
    public void SaoEquivalentes_NaoDeveEquivalerTelefoneFixo()
    {
        Assert.False(TelefoneHelper.SaoEquivalentes("551133334444", "551133334445"));
    }
}
