using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class ConfirmacaoWhatsAppTokenHelperTests
{
    private const string TokenUsuario = "NTU3OTk5ODc1NTExMQ==";

    [Fact]
    public void ExtrairTokensCandidatos_DeveExtrairTokenBase64DaMensagem()
    {
        var tokens = ConfirmacaoWhatsAppTokenHelper.ExtrairTokensCandidatos(TokenUsuario).ToList();

        Assert.Single(tokens);
        Assert.Equal(TokenUsuario, tokens[0]);
    }

    [Fact]
    public void MensagemContemTokenConfirmacao_DeveAceitarToken_QuandoTelefoneRemetenteVazio()
    {
        var contem = ConfirmacaoWhatsAppTokenHelper.MensagemContemTokenConfirmacao(TokenUsuario, string.Empty);

        Assert.True(contem);
    }

    [Fact]
    public void TokenPareceTelefoneBrasileiro_DeveValidarTokenDecodificavel()
    {
        Assert.True(ConfirmacaoWhatsAppTokenHelper.TokenPareceTelefoneBrasileiro(TokenUsuario));
        Assert.False(ConfirmacaoWhatsAppTokenHelper.TokenPareceTelefoneBrasileiro("abc"));
    }
}
