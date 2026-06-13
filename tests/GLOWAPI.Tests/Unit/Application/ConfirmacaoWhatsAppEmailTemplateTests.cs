using GLOWAPI.Application.Mensageria;

namespace GLOWAPI.Tests.Unit.Application;

public class ConfirmacaoWhatsAppEmailTemplateTests
{
    [Fact]
    public void CriarConfirmacaoJaRealizada_DeveIncluirNome()
    {
        var html = ConfirmacaoWhatsAppEmailTemplate.CriarConfirmacaoJaRealizada("Maria");

        Assert.Contains("Maria", html);
        Assert.Contains("ja esta confirmado", html);
    }

    [Fact]
    public void CriarConfirmacaoSucesso_DeveIncluirNomeETelefone()
    {
        var html = ConfirmacaoWhatsAppEmailTemplate.CriarConfirmacaoSucesso(
            "Maria",
            "5579991917634");

        Assert.Contains("Maria", html);
        Assert.Contains("5579991917634", html);
        Assert.Contains("confirmado", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_DeveIncluirLinksConfirmacaoEWhatsApp()
    {
        var html = ConfirmacaoWhatsAppEmailTemplate.Criar(
            "Maria",
            "11988887777",
            "http://localhost:3000/c/NTUxMTk4ODg4Nzc3Nw==",
            "https://wa.me/5511999999999?text=NTUxMTk4ODg4Nzc3Nw%3D%3D");

        Assert.Contains("Maria", html);
        Assert.Contains("11988887777", html);
        Assert.Contains("http://localhost:3000/c/NTUxMTk4ODg4Nzc3Nw==", html);
        Assert.Contains("https://wa.me/5511999999999?text=NTUxMTk4ODg4Nzc3Nw%3D%3D", html);
        Assert.Contains("Confirmar no WhatsApp", html);
        Assert.Contains("Abrir confirmacao", html);
    }
}
