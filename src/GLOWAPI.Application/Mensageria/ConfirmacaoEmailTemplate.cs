using System.Net;

namespace GLOWAPI.Application.Mensageria;

public enum ConfirmacaoEmailEstado
{
    AguardandoConfirmacao,
    CodigoExpirado,
    CodigoInvalido,
    ConfirmacaoRealizada
}

public static class ConfirmacaoEmailTemplate
{
    public static string Criar(
        string nome,
        string linkConfirmacao,
        string codigo,
        int validadeHoras,
        ConfirmacaoEmailEstado estado = ConfirmacaoEmailEstado.AguardandoConfirmacao)
    {
        var nomeSeguro = Html(nome);
        var validadeTexto = validadeHoras == 1 ? "1 hora" : $"{validadeHoras} horas";
        var estadoVisual = ObterEstadoVisual(estado, validadeTexto);

        var conteudoCard = EmailTemplateBlocos.Juntar(
            EmailTemplateBlocos.ParagrafoCentralizado(
                "Use o botao abaixo para confirmar automaticamente ou copie o codigo de confirmacao."),
            EmailTemplateBlocos.Botao(new EmailTemplateBotao
            {
                Texto = "Confirmar e-mail",
                Url = linkConfirmacao,
                Estilo = EmailTemplateBotaoEstilo.Primario
            }),
            EmailTemplateBlocos.DivisorOu(),
            EmailTemplateBlocos.CodigoConfirmacao(codigo),
            estadoVisual,
            EmailTemplateBlocos.LinkFallback(linkConfirmacao),
            EmailTemplateBlocos.AvisoSeguranca());

        return EmailTemplateBase.Criar(new EmailTemplateLayout
        {
            TituloPagina = "Confirme seu e-mail",
            Preheader = $"Confirme seu cadastro no GlowUp Connect com o codigo {codigo}.",
            Titulo = "Confirme seu e-mail",
            Subtitulo = $"Ola {nomeSeguro} &#128075;, finalize o seu cadastro no GlowUp Connect para acessar sua conta com seguranca.",
            ConteudoCard = conteudoCard
        });
    }

    private static string ObterEstadoVisual(ConfirmacaoEmailEstado estado, string validadeTexto) =>
        estado switch
        {
            ConfirmacaoEmailEstado.CodigoExpirado => EmailTemplateBlocos.Alerta(
                "codigo-expirado",
                "Codigo expirado",
                "Solicite um novo codigo para concluir sua confirmacao com seguranca."),
            ConfirmacaoEmailEstado.CodigoInvalido => EmailTemplateBlocos.Alerta(
                "codigo-invalido",
                "Codigo invalido",
                "Confira os numeros digitados ou use o botao de confirmacao deste e-mail."),
            ConfirmacaoEmailEstado.ConfirmacaoRealizada => EmailTemplateBlocos.Sucesso(
                "Confirmacao realizada com sucesso",
                "Seu e-mail foi confirmado. Agora voce ja pode acessar sua conta."),
            _ => EmailTemplateBlocos.Alerta(
                "codigo-ativo",
                $"Este codigo expira em <strong>{Html(validadeTexto)}</strong>",
                incluirIconeRelogio: true,
                apenasMensagemPrincipal: true)
        };

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
