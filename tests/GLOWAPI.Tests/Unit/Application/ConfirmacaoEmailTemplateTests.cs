using FluentAssertions;
using GLOWAPI.Application.Mensageria;

namespace GLOWAPI.Tests.Unit.Application;

public class ConfirmacaoEmailTemplateTests
{
    [Fact]
    public void Criar_DeveGerarTemplateResponsivoComCodigoDestacado()
    {
        var html = ConfirmacaoEmailTemplate.Criar(
            "Joao <Teste>",
            "https://app.glowupconnect.com/confirmar-email?token=abc",
            "482913",
            24);

        html.Should().Contain("<!doctype html>");
        html.Should().Contain("GlowUp Connect");
        html.Should().Contain("#ffbf00");
        html.Should().Contain("Confirmar e-mail");
        html.Should().Contain("Confirmar e-mail");
        html.Should().Contain("482913");
        html.Should().Contain("class=\"confirm-code\"");
        html.Should().Contain("@media only screen and (max-width: 620px)");
        html.Should().Contain("role=\"presentation\"");
        html.Should().Contain("Joao &lt;Teste&gt;");
    }

    [Theory]
    [InlineData(ConfirmacaoEmailEstado.CodigoExpirado, "codigo-expirado", "Codigo expirado")]
    [InlineData(ConfirmacaoEmailEstado.CodigoInvalido, "codigo-invalido", "Codigo invalido")]
    [InlineData(ConfirmacaoEmailEstado.ConfirmacaoRealizada, "confirmacao-realizada", "Confirmacao realizada com sucesso")]
    public void Criar_DeveRenderizarEstadosVisuais(
        ConfirmacaoEmailEstado estado,
        string estadoEsperado,
        string textoEsperado)
    {
        var html = ConfirmacaoEmailTemplate.Criar(
            "Maria",
            "https://app.glowupconnect.com/confirmar-email?token=abc",
            "123456",
            24,
            estado);

        html.Should().Contain($"data-confirmation-state=\"{estadoEsperado}\"");
        html.Should().Contain(textoEsperado);
    }
}
