using GLOWAPI.Application.Mensageria;

namespace GLOWAPI.Tests.Unit.Application;

public class ConfirmacaoWhatsAppEmailTemplateTests
{
    [Fact]
    public void Criar_DeveIncluirCodigoLinkWaMeETelefone()
    {
        var html = ConfirmacaoWhatsAppEmailTemplate.Criar(
            "Maria",
            "11988887777",
            "https://wa.me/5511999999999?text=GLOW%20482913",
            "482913",
            "GLOW 482913",
            24);

        Assert.Contains("Maria", html);
        Assert.Contains("11988887777", html);
        Assert.Contains("482913", html);
        Assert.Contains("https://wa.me/5511999999999?text=GLOW%20482913", html);
        Assert.Contains("Confirmar no WhatsApp", html);
        Assert.Contains("GLOW 482913", html);
    }
}
