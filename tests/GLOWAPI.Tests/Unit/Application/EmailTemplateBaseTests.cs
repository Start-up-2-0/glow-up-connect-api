using FluentAssertions;
using GLOWAPI.Application.Mensageria;

namespace GLOWAPI.Tests.Unit.Application;

public class EmailTemplateBaseTests
{
    [Fact]
    public void Criar_DeveGerarLayoutPadraoGlowUpConnect()
    {
        var html = EmailTemplateBase.Criar(new EmailTemplateLayout
        {
            TituloPagina = "Titulo teste",
            Preheader = "Resumo do e-mail",
            Titulo = "Titulo teste",
            Subtitulo = "Ola Maria, este e um teste.",
            ConteudoCard = EmailTemplateBlocos.ParagrafoCentralizado("Conteudo principal.")
        });

        html.Should().Contain("<!doctype html>");
        html.Should().Contain("GlowUp Connect");
        html.Should().Contain("#ffbf00");
        html.Should().Contain($"cid:{EmailTemplateInlineAssets.LogoContentId}");
        html.Should().Contain("Precisa de ajuda?");
        html.Should().Contain("role=\"presentation\"");
        html.Should().Contain("@media only screen and (max-width: 620px)");
    }

    [Fact]
    public void TransacionalEmailTemplate_DeveReutilizarLayoutBase()
    {
        var html = TransacionalEmailTemplate.Criar(
            "Assinatura iniciada",
            "Sua assinatura foi iniciada",
            ["Sua assinatura do plano Pro foi iniciada."]);

        html.Should().Contain("<!doctype html>");
        html.Should().Contain("Assinatura iniciada");
        html.Should().Contain("plano Pro");
        html.Should().Contain("suporte@glowupconnect.com");
    }
}
