using GLOWAPI.Application.Mensageria;

namespace GLOWAPI.Tests.Unit.Application;

public class EmailTemplateInlineAssetsTests
{
    [Fact]
    public void ResolverReferenciados_DeveRetornarAnexosReferenciadosNoHtml()
    {
        var html = ConfirmacaoEmailTemplate.Criar(
            "Maria",
            "https://app.test/confirmar-email?token=abc",
            "123456",
            24);

        var anexos = EmailTemplateInlineAssets.ResolverReferenciados(html);

        Assert.Equal(3, anexos.Count);
        Assert.Contains(anexos, a => a.ContentId == EmailTemplateInlineAssets.LogoContentId && a.Content.Length > 0);
        Assert.Contains(anexos, a => a.ContentId == EmailTemplateInlineAssets.ClockContentId && a.Content.Length > 0);
        Assert.Contains(anexos, a => a.ContentId == EmailTemplateInlineAssets.WarningContentId && a.Content.Length > 0);
    }

    [Fact]
    public void ResolverReferenciados_DeveRetornarVazio_QuandoHtmlNaoReferenciaCid()
    {
        var anexos = EmailTemplateInlineAssets.ResolverReferenciados("<p>texto simples</p>");

        Assert.Empty(anexos);
    }
}
