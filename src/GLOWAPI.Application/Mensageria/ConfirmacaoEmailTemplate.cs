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
        var linkSeguro = Html(linkConfirmacao);
        var codigoSeguro = Html(codigo);
        var validadeTexto = validadeHoras == 1 ? "1 hora" : $"{validadeHoras} horas";
        var estadoVisual = ObterEstadoVisual(estado, validadeTexto);

        return $$"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="x-apple-disable-message-reformatting">
              <title>Confirme seu cadastro no Glow Up Connect</title>
              <style>
                @media only screen and (max-width: 620px) {
                  .email-shell { width: 100% !important; }
                  .email-card { width: 100% !important; border-radius: 0 !important; }
                  .content-pad { padding: 28px 22px !important; }
                  .title { font-size: 24px !important; line-height: 31px !important; }
                  .confirm-code { font-size: 30px !important; letter-spacing: 8px !important; }
                  .button { display: block !important; width: 100% !important; box-sizing: border-box !important; }
                  .footer-pad { padding-left: 22px !important; padding-right: 22px !important; }
                }

                .button:hover {
                  background-color: #5244d9 !important;
                  box-shadow: 0 12px 24px rgba(111, 90, 240, .26) !important;
                }
              </style>
            </head>
            <body style="margin:0; padding:0; background-color:#f4f1fb; font-family:Arial, Helvetica, sans-serif; color:#241f2f;">
              <div style="display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;">
                Confirme seu cadastro no Glow Up Connect com o codigo {{codigoSeguro}}.
              </div>

              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:#f4f1fb; margin:0; padding:32px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" class="email-shell" width="600" cellspacing="0" cellpadding="0" border="0" style="width:600px; max-width:600px;">
                      <tr>
                        <td align="center" style="padding:0 0 16px;">
                          <table role="presentation" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                              <td width="44" height="44" align="center" style="background:#6f5af0; border-radius:14px; color:#ffffff; font-size:20px; font-weight:800; letter-spacing:-1px;">
                                GLOW
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <tr>
                        <td class="email-card" style="background:#ffffff; border:1px solid #e8e1f7; border-radius:20px; overflow:hidden; box-shadow:0 20px 50px rgba(57, 43, 92, .12);">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                              <td class="content-pad" style="padding:42px 46px 28px;">
                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                  <tr>
                                    <td align="center" style="padding-bottom:22px;">
                                      <div style="display:inline-block; padding:7px 12px; border-radius:999px; background:#f0ecff; color:#6f5af0; font-size:12px; font-weight:700; letter-spacing:.03em; text-transform:uppercase;">
                                        Verificacao segura
                                      </div>
                                    </td>
                                  </tr>
                                  <tr>
                                    <td align="center">
                                      <h1 class="title" style="margin:0; color:#1e1829; font-size:30px; line-height:38px; font-weight:800;">
                                        Confirme seu e-mail
                                      </h1>
                                      <p style="margin:12px 0 0; color:#6e667b; font-size:15px; line-height:24px;">
                                        Ola {{nomeSeguro}}, finalize seu cadastro no Glow Up Connect para acessar sua conta com seguranca.
                                      </p>
                                    </td>
                                  </tr>
                                </table>

                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin-top:28px;">
                                  <tr>
                                    <td style="padding:22px; border:1px solid #eee8fa; border-radius:16px; background:#fbf9ff;">
                                      <p style="margin:0 0 16px; color:#3d3549; font-size:15px; line-height:24px;">
                                        Use o botao abaixo para confirmar automaticamente ou copie o codigo de confirmacao.
                                      </p>
                                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                        <tr>
                                          <td align="center" style="padding:4px 0 22px;">
                                            <a class="button" href="{{linkSeguro}}" target="_blank" style="display:inline-block; background:#6f5af0; color:#ffffff; text-decoration:none; font-size:15px; font-weight:700; padding:14px 24px; border-radius:10px; box-shadow:0 10px 22px rgba(111, 90, 240, .22);">
                                              Confirmar e-mail
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

                                      {{estadoVisual}}

                                      <p style="margin:18px 0 0; color:#7b7288; font-size:12px; line-height:19px; text-align:center;">
                                        Se o botao nao funcionar, copie e cole este link no navegador:<br>
                                        <a href="{{linkSeguro}}" target="_blank" style="color:#6f5af0; text-decoration:underline; word-break:break-all;">{{linkSeguro}}</a>
                                      </p>
                                    </td>
                                  </tr>
                                </table>

                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin-top:24px;">
                                  <tr>
                                    <td style="padding:16px 18px; background:#f8fbf9; border:1px solid #dfeee6; border-radius:14px;">
                                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                        <tr>
                                          <td width="32" valign="top">
                                            <div style="width:24px; height:24px; border-radius:50%; background:#1f9d63; color:#ffffff; font-size:13px; line-height:24px; text-align:center; font-weight:700;">i</div>
                                          </td>
                                          <td valign="top">
                                            <p style="margin:0; color:#29513c; font-size:13px; line-height:20px; font-weight:700;">Aviso de seguranca</p>
                                            <p style="margin:4px 0 0; color:#4b6a5a; font-size:12px; line-height:19px;">
                                              Nunca compartilhe este e-mail, link ou codigo. A equipe Glow Up Connect nunca pedira sua senha.
                                            </p>
                                          </td>
                                        </tr>
                                      </table>
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>

                            <tr>
                              <td style="height:1px; background:#eee8f6;"></td>
                            </tr>

                            <tr>
                              <td class="content-pad" style="padding:28px 46px;">
                                <p style="margin:0 0 12px; color:#241f2f; font-size:15px; line-height:22px; font-weight:800;">Precisa de ajuda?</p>
                                <p style="margin:0 0 12px; color:#6e667b; font-size:13px; line-height:21px;">
                                  Nosso suporte pode ajudar com cadastro, acesso e confirmacao de conta.
                                </p>
                                <p style="margin:0; color:#6e667b; font-size:13px; line-height:23px;">
                                  E-mail: <a href="mailto:suporte@glowupconnect.com" style="color:#6f5af0; text-decoration:none;">suporte@glowupconnect.com</a><br>
                                  Atendimento: segunda a sexta, 9h as 18h
                                </p>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <tr>
                        <td class="footer-pad" align="center" style="padding:22px 34px 0;">
                          <p style="margin:0; color:#8b8398; font-size:12px; line-height:20px;">
                            Glow Up Connect<br>
                            Plataforma de agendamentos para profissionais e estabelecimentos de beleza.
                          </p>
                          <p style="margin:12px 0 0; color:#9a92a6; font-size:11px; line-height:18px;">
                            (c) {{DateTime.UtcNow.Year}} Glow Up Connect. Todos os direitos reservados.
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

    private static string ObterEstadoVisual(ConfirmacaoEmailEstado estado, string validadeTexto) =>
        estado switch
        {
            ConfirmacaoEmailEstado.CodigoExpirado => CriarEstado(
                "codigo-expirado",
                "#fff7ed",
                "#fed7aa",
                "#9a3412",
                "!",
                "Codigo expirado",
                "Solicite um novo codigo para concluir sua confirmacao com seguranca."),
            ConfirmacaoEmailEstado.CodigoInvalido => CriarEstado(
                "codigo-invalido",
                "#fef2f2",
                "#fecaca",
                "#991b1b",
                "!",
                "Codigo invalido",
                "Confira os numeros digitados ou use o botao de confirmacao deste e-mail."),
            ConfirmacaoEmailEstado.ConfirmacaoRealizada => CriarEstado(
                "confirmacao-realizada",
                "#f0fdf4",
                "#bbf7d0",
                "#166534",
                "OK",
                "Confirmacao realizada com sucesso",
                "Seu e-mail foi confirmado. Agora voce ja pode acessar sua conta."),
            _ => CriarEstado(
                "codigo-ativo",
                "#f5f3ff",
                "#ddd6fe",
                "#5b21b6",
                "24",
                "Codigo ativo",
                $"Este codigo expira em {validadeTexto}.")
        };

    private static string CriarEstado(
        string id,
        string background,
        string border,
        string color,
        string icon,
        string title,
        string description) =>
        $"""
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" data-confirmation-state="{id}" style="margin-top:16px;">
          <tr>
            <td style="padding:13px 14px; background:{background}; border:1px solid {border}; border-radius:12px;">
              <table role="presentation" cellspacing="0" cellpadding="0" border="0">
                <tr>
                  <td width="30" valign="top">
                    <div style="width:22px; height:22px; border-radius:50%; background:{color}; color:#ffffff; font-size:10px; line-height:22px; text-align:center; font-weight:800;">{Html(icon)}</div>
                  </td>
                  <td valign="top">
                    <p style="margin:0; color:{color}; font-size:13px; line-height:19px; font-weight:800;">{Html(title)}</p>
                    <p style="margin:3px 0 0; color:{color}; font-size:12px; line-height:18px;">{Html(description)}</p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
        </table>
        """;

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
