using System.Net;

namespace GLOWAPI.Application.Mensageria;

public static class ConfirmacaoWhatsAppEmailTemplate
{
    public static string CriarFalhaConfirmacao(string nome) =>
        TransacionalEmailTemplate.CriarComSaudacao(
            titulo: "Nao conseguimos confirmar seu WhatsApp",
            preheader: "Nao conseguimos confirmar seu WhatsApp no Glow Up Connect.",
            nome: nome,
            paragrafos:
            [
                "Nao conseguimos confirmar seu WhatsApp no Glow Up Connect.",
                "Verifique o link mais recente no WhatsApp ou e-mail de confirmacao ou solicite uma nova confirmacao pelo app.",
                "Se o problema persistir, confira se a mensagem foi enviada do mesmo numero cadastrado no perfil."
            ]);

    public static string CriarFalhaConfirmacaoGenerica() =>
        TransacionalEmailTemplate.Criar(
            titulo: "Nao conseguimos confirmar seu WhatsApp",
            preheader: "Nao conseguimos confirmar seu WhatsApp no Glow Up Connect.",
            paragrafos:
            [
                "Nao conseguimos confirmar seu WhatsApp no Glow Up Connect.",
                "Verifique se o telefone esta cadastrado no perfil e use o link mais recente enviado por WhatsApp ou e-mail."
            ]);

    public static string CriarConfirmacaoSucesso(string nome, string telefonePerfil) =>
        TransacionalEmailTemplate.CriarComSaudacao(
            titulo: "WhatsApp confirmado",
            preheader: "Seu WhatsApp foi confirmado no Glow Up Connect.",
            nome: nome,
            paragrafos:
            [
                $"Seu WhatsApp {telefonePerfil} foi confirmado no Glow Up Connect.",
                "Voce passara a receber alertas de agendamento por este numero."
            ],
            destaqueTitulo: "Confirmacao realizada com sucesso",
            destaqueDescricao: "Seu WhatsApp foi confirmado com sucesso.");

    public static string CriarConfirmacaoJaRealizada(string nome) =>
        TransacionalEmailTemplate.CriarComSaudacao(
            titulo: "WhatsApp ja confirmado",
            preheader: "Seu WhatsApp ja esta confirmado no Glow Up Connect.",
            nome: nome,
            paragrafos:
            [
                "Seu WhatsApp ja esta confirmado no Glow Up Connect.",
                "Nao e necessario enviar uma nova mensagem de confirmacao."
            ]);

    public static string Criar(
        string nome,
        string telefonePerfil,
        string linkConfirmacao,
        string linkWhatsApp)
    {
        var nomeSeguro = Html(nome);
        var telefoneSeguro = Html(telefonePerfil);

        var conteudoCard = EmailTemplateBlocos.Juntar(
            EmailTemplateBlocos.ParagrafoCentralizado(
                "Toque no botao abaixo para abrir a pagina de confirmacao. Em seguida, envie a mensagem pelo WhatsApp usando o numero cadastrado no perfil."),
            EmailTemplateBlocos.Botao(new EmailTemplateBotao
            {
                Texto = "Abrir confirmacao",
                Url = linkConfirmacao,
                Estilo = EmailTemplateBotaoEstilo.Link
            }),
            EmailTemplateBlocos.DivisorOu(),
            EmailTemplateBlocos.Botao(new EmailTemplateBotao
            {
                Texto = "Confirmar no WhatsApp",
                Url = linkWhatsApp,
                Estilo = EmailTemplateBotaoEstilo.WhatsApp
            }),
            EmailTemplateBlocos.LinkTextoSimples(linkConfirmacao),
            EmailTemplateBlocos.AvisoSeguranca("Nunca compartilhe este e-mail ou link. A equipe GlowUp Connect nunca pedira a sua senha."));

        return EmailTemplateBase.Criar(new EmailTemplateLayout
        {
            TituloPagina = "Confirme seu WhatsApp",
            Preheader = "Confirme seu WhatsApp no GlowUp Connect.",
            Titulo = "Confirme seu WhatsApp",
            Subtitulo = $"Ola {nomeSeguro} &#128075;, confirme o numero <strong>{telefoneSeguro}</strong> para receber alertas de agendamento.",
            ConteudoCard = conteudoCard
        });
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
