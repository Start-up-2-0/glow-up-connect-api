using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Mensageria;

public static class ConfirmacaoWhatsAppInstrucoesBuilder
{
    public static WhatsAppConfirmacaoInstrucoesDto Criar(
        MensageriaWhatsAppOptions whatsAppOptions,
        string codigoPlano,
        DateTime expiraEm,
        bool emailEnviado)
    {
        var numeroPlataforma = ObterNumeroPlataformaExibicao(whatsAppOptions);
        var mensagemSugerida = ConfirmacaoWhatsAppTemplate.MensagemSugeridaInbound(codigoPlano);
        var linkWhatsApp = TelefoneHelper.CriarLinkWaMe(numeroPlataforma, mensagemSugerida);

        return new WhatsAppConfirmacaoInstrucoesDto
        {
            NumeroPlataforma = numeroPlataforma,
            CodigoConfirmacao = codigoPlano,
            MensagemSugerida = mensagemSugerida,
            LinkWhatsApp = linkWhatsApp,
            EmailEnviado = emailEnviado,
            ExpiraEm = expiraEm
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
