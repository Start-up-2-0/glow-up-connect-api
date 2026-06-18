using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Options;

namespace GLOWAPI.Application.Mensageria;

public static class ConfirmacaoWhatsAppInstrucoesBuilder
{
    public static WhatsAppConfirmacaoInstrucoesDto Criar(
        MensageriaWhatsAppOptions whatsAppOptions,
        AuthOptions authOptions,
        string telefone,
        bool whatsAppEnviado,
        bool emailEnviado)
    {
        var numeroPlataforma = ObterNumeroPlataformaExibicao(whatsAppOptions);
        var tokenConfirmacao = TelefoneHelper.GerarTokenConfirmacao(telefone);
        var linkConfirmacao = TelefoneHelper.CriarLinkConfirmacao(authOptions.FrontendBaseUrl, tokenConfirmacao);
        var linkWhatsApp = TelefoneHelper.CriarLinkWaMe(numeroPlataforma, tokenConfirmacao);

        return new WhatsAppConfirmacaoInstrucoesDto
        {
            NumeroPlataforma = numeroPlataforma,
            TokenConfirmacao = tokenConfirmacao,
            LinkConfirmacao = linkConfirmacao,
            LinkWhatsApp = linkWhatsApp,
            WhatsAppEnviado = whatsAppEnviado,
            EmailEnviado = emailEnviado
        };
    }

    private static string ObterNumeroPlataformaExibicao(MensageriaWhatsAppOptions options)
    {
        var numero = options.NumeroPlataforma.Trim();
        return string.IsNullOrWhiteSpace(numero)
            ? "numero da plataforma (configure Mensageria:WhatsApp:NumeroPlataforma)"
            : numero;
    }
}
