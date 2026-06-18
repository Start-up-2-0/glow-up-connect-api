using System.Net;

namespace GLOWAPI.Application.Mensageria;

public static class TransacionalEmailTemplate
{
    public static string Criar(
        string titulo,
        string preheader,
        IEnumerable<string> paragrafos,
        EmailTemplateBotao? botao = null,
        string? destaqueTitulo = null,
        string? destaqueDescricao = null,
        bool incluirAvisoSeguranca = false,
        string? linkFallback = null)
    {
        var paragrafosHtml = string.Join(
            string.Empty,
            paragrafos.Select(EmailTemplateBlocos.ParagrafoCentralizado));

        var destaqueHtml = string.IsNullOrWhiteSpace(destaqueTitulo)
            ? null
            : EmailTemplateBlocos.Sucesso(destaqueTitulo, destaqueDescricao ?? string.Empty);

        var conteudoCard = EmailTemplateBlocos.Juntar(
            paragrafosHtml,
            botao is null ? null : EmailTemplateBlocos.Botao(botao),
            destaqueHtml,
            string.IsNullOrWhiteSpace(linkFallback) ? null : EmailTemplateBlocos.LinkFallback(linkFallback),
            incluirAvisoSeguranca ? EmailTemplateBlocos.AvisoSeguranca() : null);

        return EmailTemplateBase.Criar(new EmailTemplateLayout
        {
            TituloPagina = titulo,
            Preheader = preheader,
            Titulo = titulo,
            ConteudoCard = conteudoCard
        });
    }

    public static string CriarComSaudacao(
        string titulo,
        string preheader,
        string nome,
        IEnumerable<string> paragrafos,
        EmailTemplateBotao? botao = null,
        string? destaqueTitulo = null,
        string? destaqueDescricao = null,
        bool incluirAvisoSeguranca = false,
        string? linkFallback = null) =>
        Criar(
            titulo,
            preheader,
            new[] { $"Ola {nome}," }.Concat(paragrafos),
            botao,
            destaqueTitulo,
            destaqueDescricao,
            incluirAvisoSeguranca,
            linkFallback);

    public static string CriarComSubtitulo(
        string titulo,
        string preheader,
        string subtituloHtml,
        IEnumerable<string> paragrafos,
        EmailTemplateBotao? botao = null,
        string? destaqueHtml = null,
        bool incluirAvisoSeguranca = false,
        string? linkFallback = null,
        string? linkTextoSimples = null)
    {
        var paragrafosHtml = string.Join(
            string.Empty,
            paragrafos.Select(EmailTemplateBlocos.ParagrafoCentralizado));

        var conteudoCard = EmailTemplateBlocos.Juntar(
            paragrafosHtml,
            botao is null ? null : EmailTemplateBlocos.Botao(botao),
            destaqueHtml,
            string.IsNullOrWhiteSpace(linkFallback) ? null : EmailTemplateBlocos.LinkFallback(linkFallback),
            string.IsNullOrWhiteSpace(linkTextoSimples) ? null : EmailTemplateBlocos.LinkTextoSimples(linkTextoSimples),
            incluirAvisoSeguranca ? EmailTemplateBlocos.AvisoSeguranca() : null);

        return EmailTemplateBase.Criar(new EmailTemplateLayout
        {
            TituloPagina = titulo,
            Preheader = preheader,
            Titulo = titulo,
            Subtitulo = subtituloHtml,
            ConteudoCard = conteudoCard
        });
    }

    public static string HtmlEncode(string value) => WebUtility.HtmlEncode(value);
}
