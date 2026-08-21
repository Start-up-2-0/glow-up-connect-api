using System.Net;
using System.Text;

namespace GLOWAPI.Application.Mensageria;

public enum EmailTemplateBotaoEstilo
{
    Primario,
    Link,
    WhatsApp
}

public sealed class EmailTemplateBotao
{
    public required string Texto { get; init; }
    public required string Url { get; init; }
    public EmailTemplateBotaoEstilo Estilo { get; init; } = EmailTemplateBotaoEstilo.Primario;
}

public sealed class EmailTemplateLayout
{
    public required string TituloPagina { get; init; }
    public required string Preheader { get; init; }
    public required string Titulo { get; init; }
    public string? Subtitulo { get; init; }
    public required string ConteudoCard { get; init; }
}

public static class EmailTemplateBlocos
{
    public static string ParagrafoCentralizado(string texto) =>
        $"""
            <p style="margin:0 0 16px; color:{EmailTemplateCores.Texto}; font-size:18px; line-height:26px; text-align:center;">
              {Html(texto)}
            </p>
            """;

    public static string ParagrafoHtml(string htmlSeguro) =>
        $"""
            <div style="margin:0 0 16px; color:{EmailTemplateCores.Texto}; font-size:18px; line-height:26px; text-align:center;">
              {htmlSeguro}
            </div>
            """;

    public static string Botao(EmailTemplateBotao botao)
    {
        var (fundo, texto) = botao.Estilo switch
        {
            EmailTemplateBotaoEstilo.Link => (EmailTemplateCores.Link, EmailTemplateCores.Superficie),
            EmailTemplateBotaoEstilo.WhatsApp => (EmailTemplateCores.WhatsApp, EmailTemplateCores.Superficie),
            _ => (EmailTemplateCores.Accent, EmailTemplateCores.Superficie)
        };

        var urlSegura = Html(botao.Url);
        var textoSeguro = Html(botao.Texto);

        return $"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
              <tr>
                <td align="center" style="padding:0 0 20px;">
                  <a class="button" href="{urlSegura}" target="_blank" style="display:inline-block; background:{fundo}; color:{texto}; text-decoration:none; font-size:18px; font-weight:700; padding:14px 32px; border-radius:12px; min-width:220px; text-align:center;">
                    {textoSeguro}
                  </a>
                </td>
              </tr>
            </table>
            """;
    }

    public static string DivisorOu() =>
        $"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin:0 0 20px;">
              <tr>
                <td width="42%" style="border-top:1px solid {EmailTemplateCores.BordaMedia}; font-size:0; line-height:0;">&nbsp;</td>
                <td align="center" style="padding:0 12px; color:{EmailTemplateCores.TextoSuave}; font-size:18px; line-height:18px; opacity:0.4; white-space:nowrap;">ou</td>
                <td width="42%" style="border-top:1px solid {EmailTemplateCores.BordaMedia}; font-size:0; line-height:0;">&nbsp;</td>
              </tr>
            </table>
            """;

    public static string CodigoConfirmacao(string codigo, string rotulo = "Codigo de confirmacao") =>
        $"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin-bottom:20px;">
              <tr>
                <td align="center" style="padding:22px 16px; background:{EmailTemplateCores.SuperficieEscura}; border:2px dashed {EmailTemplateCores.DestaqueClaro}; border-radius:20px;">
                  <div style="color:{EmailTemplateCores.DestaqueClaro}; font-size:14px; font-weight:300; letter-spacing:0.04em; text-transform:uppercase;">
                    {Html(rotulo)}
                  </div>
                  <div class="confirm-code" style="margin-top:10px; color:{EmailTemplateCores.DestaqueClaro}; font-size:32px; line-height:40px; font-weight:500; letter-spacing:7px;">
                    {Html(codigo)}
                  </div>
                </td>
              </tr>
            </table>
            """;

    public static string Alerta(
        string estadoId,
        string titulo,
        string? descricao = null,
        bool incluirIconeRelogio = false,
        bool apenasMensagemPrincipal = false)
    {
        var clockSrc = EmailTemplateInlineAssets.ClockSrc;
        var icone = incluirIconeRelogio
            ? $"""<img src="{clockSrc}" alt="" width="20" height="20" style="display:block; width:20px; height:20px; border:0;">"""
            : string.Empty;

        var descricaoHtml = string.IsNullOrEmpty(descricao)
            ? string.Empty
            : $"""<p style="margin:4px 0 0; color:{EmailTemplateCores.Alerta}; font-size:12px; line-height:18px;">{Html(descricao)}</p>""";

        var tituloHtml = apenasMensagemPrincipal
            ? $"""<p style="margin:0; color:{EmailTemplateCores.Alerta}; font-size:12px; line-height:18px;">{titulo}</p>"""
            : $"""<p style="margin:0; color:{EmailTemplateCores.Alerta}; font-size:12px; line-height:18px; font-weight:700;">{Html(titulo)}</p>{descricaoHtml}""";

        return $"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" data-confirmation-state="{estadoId}" style="margin-bottom:20px;">
              <tr>
                <td style="padding:12px 14px; border:0.5px solid {EmailTemplateCores.Alerta}; border-radius:12px;">
                  <table role="presentation" cellspacing="0" cellpadding="0" border="0">
                    <tr>
                      {(incluirIconeRelogio ? $"""<td width="30" valign="middle">{icone}</td>""" : string.Empty)}
                      <td valign="middle">
                        {tituloHtml}
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
            """;
    }

    public static string Sucesso(string titulo, string descricao) =>
        $"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" data-confirmation-state="confirmacao-realizada" style="margin-bottom:20px;">
              <tr>
                <td style="padding:12px 14px; border:0.5px solid {EmailTemplateCores.BordaSeguranca}; border-radius:12px;">
                  <p style="margin:0; color:{EmailTemplateCores.Seguranca}; font-size:12px; line-height:18px; font-weight:700;">{Html(titulo)}</p>
                  <p style="margin:4px 0 0; color:{EmailTemplateCores.Seguranca}; font-size:12px; line-height:18px;">{Html(descricao)}</p>
                </td>
              </tr>
            </table>
            """;

    public static string LinkFallback(string url, string? instrucao = null)
    {
        var urlSegura = Html(url);
        var textoInstrucao = instrucao ?? "Se o botao nao funcionar, copie e cole este link no navegador:";

        return $"""
            <p style="margin:20px 0 8px; color:{EmailTemplateCores.TextoSuave}; font-size:14px; line-height:22px; text-align:center;">
              {Html(textoInstrucao)}
            </p>
            <p style="margin:0; text-align:center;">
              <a href="{urlSegura}" target="_blank" style="color:{EmailTemplateCores.Link}; font-size:14px; line-height:22px; text-decoration:underline; word-break:break-all;">{urlSegura}</a>
            </p>
            """;
    }

    public static string LinkTextoSimples(string url, string? instrucao = null)
    {
        var urlSegura = Html(url);
        var textoInstrucao = instrucao ?? "Se os botoes nao funcionarem, copie e abra este link no celular:";

        return $"""
            <p style="margin:0; color:{EmailTemplateCores.TextoSuave}; font-size:12px; line-height:19px; text-align:center;">
              {Html(textoInstrucao)} {urlSegura}
            </p>
            """;
    }

    public static string AvisoSeguranca(string mensagem = "Nunca compartilhe este e-mail, link ou codigo. A equipe GlowUp Connect nunca pedira a sua senha.") =>
        $"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin-top:24px;">
              <tr>
                <td style="padding:16px 18px; border:0.5px solid {EmailTemplateCores.BordaSeguranca}; border-radius:12px;">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                    <tr>
                      <td width="28" valign="top" style="padding-top:2px;">
                        <img src="{EmailTemplateInlineAssets.WarningSrc}" alt="" width="16" height="16" style="display:block; width:16px; height:16px; border:0;">
                      </td>
                      <td valign="top">
                        <p style="margin:0; color:{EmailTemplateCores.Seguranca}; font-size:14px; line-height:20px; font-weight:700;">Aviso de seguranca</p>
                        <p style="margin:6px 0 0; color:{EmailTemplateCores.Seguranca}; font-size:12px; line-height:18px; font-weight:300;">
                          {Html(mensagem)}
                        </p>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
            """;

    public static string Juntar(params string?[] blocos)
    {
        var builder = new StringBuilder();
        foreach (var bloco in blocos)
        {
            if (!string.IsNullOrWhiteSpace(bloco))
            {
                builder.Append(bloco);
            }
        }

        return builder.ToString();
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
