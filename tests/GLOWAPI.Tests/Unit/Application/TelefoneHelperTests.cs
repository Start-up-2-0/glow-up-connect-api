using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class TelefoneHelperTests
{
    [Fact]
    public void CriarLinkWaMe_DeveGerarUrlComTextoEscapado()
    {
        var link = TelefoneHelper.CriarLinkWaMe("5511999999999", "NTUxMTk4ODg4Nzc3Nw==");

        Assert.Contains("wa.me/5511999999999", link);
        Assert.Contains("text=NTUxMTk4ODg4Nzc3Nw%3D%3D", link);
    }

    [Fact]
    public void GerarTokenConfirmacao_DeveGerarBase64DoTelefoneNormalizado()
    {
        var token = TelefoneHelper.GerarTokenConfirmacao("11988887777");

        Assert.Equal("NTUxMTk4ODg4Nzc3Nw==", token);
    }

    [Fact]
    public void GerarTokenConfirmacao_DeveInserirNonoDigito_QuandoTelefoneTem12Digitos()
    {
        var token = TelefoneHelper.GerarTokenConfirmacao("551188887777");

        Assert.Equal("NTUxMTk4ODg4Nzc3Nw==", token);
    }

    [Fact]
    public void NormalizarParaConfirmacaoInbound_DeveInserirNonoDigito()
    {
        var normalizado = TelefoneHelper.NormalizarParaConfirmacaoInbound("551188887777");

        Assert.Equal("5511988887777", normalizado);
    }

    [Fact]
    public void CriarLinkConfirmacao_DeveMontarUrlPublica()
    {
        var link = TelefoneHelper.CriarLinkConfirmacao(
            "http://localhost:3000/",
            "NTUxMTk4ODg4Nzc3Nw==");

        Assert.Equal("http://localhost:3000/c/NTUxMTk4ODg4Nzc3Nw==", link);
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

    [Fact]
    public void NormalizarParaArmazenamento_DeveAdicionarDdi55_QuandoTelefoneLocal()
    {
        Assert.Equal("5511999999999", TelefoneHelper.NormalizarParaArmazenamento("11999999999"));
        Assert.Equal("5511999999999", TelefoneHelper.NormalizarParaArmazenamento("(11) 99999-9999"));
    }

    [Fact]
    public void NormalizarParaArmazenamento_DeveManterDdi55_QuandoJaInformado()
    {
        Assert.Equal("5511999999999", TelefoneHelper.NormalizarParaArmazenamento("5511999999999"));
    }

    [Fact]
    public void NormalizarParaArmazenamento_DeveRetornarVazio_QuandoNuloOuBranco()
    {
        Assert.Equal(string.Empty, TelefoneHelper.NormalizarParaArmazenamento(null));
        Assert.Equal(string.Empty, TelefoneHelper.NormalizarParaArmazenamento("   "));
    }
}
