using System.Net;

namespace GLOWAPI.Application.Mensageria;

public static class ConfirmacaoWhatsAppEmailTemplate
{
    public static string Criar(
        string nome,
        string telefonePerfil,
        string linkWhatsApp,
        string codigo,
        string mensagemSugerida,
        int validadeHoras)
    {
        var nomeSeguro = Html(nome);
        var telefoneSeguro = Html(telefonePerfil);
        var linkSeguro = Html(linkWhatsApp);
        var codigoSeguro = Html(codigo);
        var mensagemSegura = Html(mensagemSugerida);
        var validadeTexto = validadeHoras == 1 ? "1 hora" : $"{validadeHoras} horas";

        return $$"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="x-apple-disable-message-reformatting">
              <title>Confirme seu WhatsApp no Glow Up Connect</title>
              <style>
                @media only screen and (max-width: 620px) {
                  .email-shell { width: 100% !important; }
                  .email-card { width: 100% !important; border-radius: 0 !important; }
                  .content-pad { padding: 28px 22px !important; }
                  .title { font-size: 24px !important; line-height: 31px !important; }
                  .confirm-code { font-size: 30px !important; letter-spacing: 8px !important; }
                  .button { display: block !important; width: 100% !important; box-sizing: border-box !important; }
                }
                .button:hover {
                  background-color: #128c7e !important;
                }
              </style>
            </head>
            <body style="margin:0; padding:0; background-color:#f4f1fb; font-family:Arial, Helvetica, sans-serif; color:#241f2f;">
              <div style="display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;">
                Confirme seu WhatsApp no Glow Up Connect. Codigo {{codigoSeguro}}.
              </div>
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:#f4f1fb; margin:0; padding:32px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" class="email-shell" width="600" cellspacing="0" cellpadding="0" border="0" style="width:600px; max-width:600px;">
                      <tr>
                        <td align="center" style="padding:0 0 16px;">
                          <div style="display:inline-block; padding:10px 16px; background:#25d366; border-radius:14px; color:#ffffff; font-size:14px; font-weight:800;">
                            WhatsApp
                          </div>
                        </td>
                      </tr>
                      <tr>
                        <td class="email-card" style="background:#ffffff; border:1px solid #e8e1f7; border-radius:20px; overflow:hidden; box-shadow:0 20px 50px rgba(57, 43, 92, .12);">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                              <td class="content-pad" style="padding:42px 46px 28px;">
                                <h1 class="title" style="margin:0; color:#1e1829; font-size:28px; line-height:36px; font-weight:800; text-align:center;">
                                  Confirme seu WhatsApp
                                </h1>
                                <p style="margin:12px 0 0; color:#6e667b; font-size:15px; line-height:24px; text-align:center;">
                                  Ola {{nomeSeguro}}, confirme o numero <strong>{{telefoneSeguro}}</strong> para receber alertas de agendamento.
                                </p>
                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin-top:28px;">
                                  <tr>
                                    <td style="padding:22px; border:1px solid #eee8fa; border-radius:16px; background:#fbf9ff;">
                                      <p style="margin:0 0 16px; color:#3d3549; font-size:15px; line-height:24px; text-align:center;">
                                        Toque no botao abaixo para abrir o WhatsApp da plataforma com a mensagem pronta. Envie usando o numero cadastrado no perfil.
                                      </p>
                                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                        <tr>
                                          <td align="center" style="padding:4px 0 22px;">
                                            <a class="button" href="{{linkSeguro}}" target="_blank" style="display:inline-block; background:#25d366; color:#ffffff; text-decoration:none; font-size:15px; font-weight:700; padding:14px 24px; border-radius:10px;">
                                              Confirmar no WhatsApp
                                            </a>
                                          </td>
                                        </tr>
                                      </table>
                                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                        <tr>
                                          <td align="center" style="padding:18px 12px; background:#ffffff; border:1px dashed #cfc4f6; border-radius:14px;">
                                            <div style="color:#887f96; font-size:12px; font-weight:700; letter-spacing:.04em; text-transform:uppercase;">
                                              Codigo de confirmacao
                                            </div>
                                            <div class="confirm-code" style="margin-top:8px; color:#241f2f; font-size:36px; line-height:44px; font-weight:800; letter-spacing:10px; font-family:'Courier New', Courier, monospace;">
                                              {{codigoSeguro}}
                                            </div>
                                          </td>
                                        </tr>
                                      </table>
                                      <p style="margin:18px 0 0; color:#7b7288; font-size:13px; line-height:20px; text-align:center;">
                                        Mensagem sugerida: <strong>{{mensagemSegura}}</strong><br>
                                        Valido por {{validadeTexto}}.
                                      </p>
                                      <p style="margin:14px 0 0; color:#7b7288; font-size:12px; line-height:19px; text-align:center;">
                                        Se o botao nao funcionar, copie a mensagem e envie manualmente para o WhatsApp da plataforma.
                                      </p>
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>
                          </table>
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
