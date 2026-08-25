using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class ConfirmacaoWhatsAppTokenHelperTests
{
    private static readonly string TokenUsuario = ConfirmacaoWhatsAppTokenHelper.Gerar(
        9, ConfirmacaoWhatsAppTokenHelper.TipoConta, "5579998755111");

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

    [Fact]
    public void TentarDecodificar_DeveConterIdTipoETelefone()
    {
        Assert.True(ConfirmacaoWhatsAppTokenHelper.TentarDecodificar(TokenUsuario, out var payload));
        Assert.Equal(9, payload!.Id);
        Assert.Equal("account", payload.Type);
        Assert.Equal("5579998755111", payload.Phone);
    }
}
