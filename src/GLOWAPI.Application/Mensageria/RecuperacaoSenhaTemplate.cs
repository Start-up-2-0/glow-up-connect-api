using System.Net;

namespace GLOWAPI.Application.Mensageria;

public static class RecuperacaoSenhaTemplate
{
    public static string Criar(
        string nome,
        string linkRedefinicao,
        string codigo,
        int validadeMinutos)
    {
        var nomeSeguro = Html(nome);
        var validadeTexto = validadeMinutos == 1 ? "1 minuto" : $"{validadeMinutos} minutos";

        var conteudoCard = EmailTemplateBlocos.Juntar(
            EmailTemplateBlocos.ParagrafoCentralizado(
                "Use o botao abaixo para redefinir sua senha ou copie o codigo de recuperacao."),
            EmailTemplateBlocos.Botao(new EmailTemplateBotao
            {
                Texto = "Redefinir senha",
                Url = linkRedefinicao,
                Estilo = EmailTemplateBotaoEstilo.Primario
            }),
            EmailTemplateBlocos.DivisorOu(),
            EmailTemplateBlocos.CodigoConfirmacao(codigo, "Codigo de recuperacao"),
            EmailTemplateBlocos.Alerta(
                "codigo-ativo",
                $"Este codigo expira em <strong>{Html(validadeTexto)}</strong>",
                incluirIconeRelogio: true,
                apenasMensagemPrincipal: true),
            EmailTemplateBlocos.LinkFallback(linkRedefinicao),
            EmailTemplateBlocos.AvisoSeguranca());

        return EmailTemplateBase.Criar(new EmailTemplateLayout
        {
            TituloPagina = "Redefina sua senha",
            Preheader = $"Redefina sua senha no GlowUp Connect com o codigo {codigo}.",
            Titulo = "Redefina sua senha",
            Subtitulo = $"Ola {nomeSeguro} &#128075;, recebemos um pedido para redefinir a senha da sua conta GlowUp Connect.",
            ConteudoCard = conteudoCard
        });
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
