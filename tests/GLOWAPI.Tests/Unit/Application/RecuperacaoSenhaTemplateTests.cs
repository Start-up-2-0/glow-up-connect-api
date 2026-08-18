using FluentAssertions;
using GLOWAPI.Application.Mensageria;

namespace GLOWAPI.Tests.Unit.Application;

public class RecuperacaoSenhaTemplateTests
{
    [Fact]
    public void Criar_DeveGerarTemplateResponsivoComCodigoDestacado()
    {
        var html = RecuperacaoSenhaTemplate.Criar(
            "Joao <Teste>",
            "https://app.glowupconnect.com/resetar-senha?token=abc",
            "482913",
            30);

        html.Should().Contain("<!doctype html>");
        html.Should().Contain("GlowUp Connect");
        html.Should().Contain("#ffbf00");
        html.Should().Contain("Redefinir senha");
        html.Should().Contain($"cid:{EmailTemplateInlineAssets.LogoContentId}");
        html.Should().NotContain("data:image");
        html.Should().Contain("482913");
        html.Should().Contain("class=\"confirm-code\"");
        html.Should().Contain("30 minutos");
        html.Should().Contain("@media only screen and (max-width: 620px)");
        html.Should().Contain("role=\"presentation\"");
        html.Should().Contain("Joao &lt;Teste&gt;");
        html.Should().Contain("/resetar-senha?token=abc");
    }
}
