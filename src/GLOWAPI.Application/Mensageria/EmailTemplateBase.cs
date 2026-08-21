using System.Net;

namespace GLOWAPI.Application.Mensageria;

public static class EmailTemplateBase
{
    public static string Criar(EmailTemplateLayout layout)
    {
        var tituloPagina = Html(layout.TituloPagina);
        var preheader = Html(layout.Preheader);
        var titulo = Html(layout.Titulo);
        var subtituloHtml = string.IsNullOrWhiteSpace(layout.Subtitulo)
            ? string.Empty
            : $"""
                <p style="margin:16px 0 0; color:{EmailTemplateCores.TextoSuave}; font-size:18px; line-height:26px; text-align:center;">
                  {layout.Subtitulo}
                </p>
                """;
        var logoSrc = EmailTemplateInlineAssets.LogoSrc;
        var anoAtual = DateTime.UtcNow.Year;

        return $$"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="x-apple-disable-message-reformatting">
              <title>{{tituloPagina}} - GlowUp Connect</title>
              <style>
                @media only screen and (max-width: 620px) {
                  .email-shell { width: 100% !important; }
                  .email-card { width: 100% !important; border-radius: 0 !important; }
                  .content-pad { padding: 24px 20px !important; }
                  .title { font-size: 32px !important; line-height: 38px !important; }
                  .confirm-code { font-size: 28px !important; letter-spacing: 6px !important; }
                  .button { display: block !important; width: 100% !important; box-sizing: border-box !important; }
                  .header-logo { width: 96px !important; height: 96px !important; }
                  .header-banner { height: auto !important; min-height: 120px !important; }
                }
              </style>
            </head>
            <body style="margin:0; padding:0; background-color:{{EmailTemplateCores.Canvas}}; font-family:'Poppins', Arial, Helvetica, sans-serif; color:{{EmailTemplateCores.Texto}};">
              <div style="display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;">
                {{preheader}}
              </div>

              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:{{EmailTemplateCores.Canvas}}; margin:0; padding:24px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" class="email-shell" width="600" cellspacing="0" cellpadding="0" border="0" style="width:600px; max-width:600px;">
                      <tr>
                        <td class="email-card" style="background:{{EmailTemplateCores.Superficie}}; border:1px solid {{EmailTemplateCores.Borda}}; border-radius:20px; overflow:hidden;">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                              <td class="header-banner" style="background-color:{{EmailTemplateCores.Accent}}; padding:0; position:relative;">
                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                  <tr>
                                    <td width="130" valign="middle" style="padding:12px 0 12px 8px;">
                                      <img class="header-logo" src="{{logoSrc}}" alt="GlowUp Connect" width="120" height="120" style="display:block; width:120px; height:120px; border:0;">
                                    </td>
                                    <td align="right" valign="middle" style="padding:24px 28px 24px 12px;">
                                      <p style="margin:0; color:#ffffff; font-family:'Montserrat', Arial, Helvetica, sans-serif; font-size:32px; line-height:35px; font-weight:500;">GlowUp</p>
                                      <p style="margin:0; color:#ffffff; font-family:'Montserrat', Arial, Helvetica, sans-serif; font-size:36px; line-height:39px; font-weight:500;">Connect</p>
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>

                            <tr>
                              <td class="content-pad" style="padding:32px 40px 12px;">
                                <h1 class="title" style="margin:0; color:{{EmailTemplateCores.Texto}}; font-family:'Montserrat', Arial, Helvetica, sans-serif; font-size:48px; line-height:52px; font-weight:300; text-align:center;">
                                  {{titulo}}
                                </h1>
                                {{subtituloHtml}}
                              </td>
                            </tr>

                            <tr>
                              <td class="content-pad" style="padding:12px 32px 32px;">
                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background:rgba(255,255,255,0.6); border:0.5px solid {{EmailTemplateCores.Borda}}; border-radius:20px;">
                                  <tr>
                                    <td style="padding:32px 28px;">
                                      {{layout.ConteudoCard}}
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>

                            <tr>
                              <td style="height:1px; background:{{EmailTemplateCores.Divisor}}; margin:0 32px;"></td>
                            </tr>

                            <tr>
                              <td class="content-pad" style="padding:28px 40px 36px;">
                                <p style="margin:0 0 12px; color:{{EmailTemplateCores.Texto}}; font-size:18px; line-height:24px; font-weight:600;">Precisa de ajuda?</p>
                                <p style="margin:0 0 12px; color:{{EmailTemplateCores.TextoSuave}}; font-size:12px; line-height:20px;">
                                  Nosso suporte pode ajudar com cadastro, acesso e confirmacao de conta.
                                </p>
                                <p style="margin:0 0 8px; color:{{EmailTemplateCores.TextoSuave}}; font-size:12px; line-height:20px;">
                                  E-mail: <a href="mailto:suporte@glowupconnect.com" style="color:{{EmailTemplateCores.Link}}; text-decoration:none;">suporte@glowupconnect.com</a>
                                </p>
                                <p style="margin:0; color:{{EmailTemplateCores.TextoSuave}}; font-size:12px; line-height:20px;">
                                  Atendimento: segunda a sexta, das 9h as 18h.
                                </p>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <tr>
                        <td align="center" style="padding:18px 20px 0;">
                          <p style="margin:0; color:{{EmailTemplateCores.TextoSuave}}; font-size:11px; line-height:18px;">
                            (c) {{anoAtual}} GlowUp Connect. Todos os direitos reservados.
                          </p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
