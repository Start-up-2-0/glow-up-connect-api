using System.Net;

namespace GLOWAPI.Application.Mensageria;

public static class ConfirmacaoWhatsAppEmailTemplate
{
    public static string CriarFalhaConfirmacao(string nome) =>
        $"""
            <p>Ola {Html(nome)},</p>
            <p>Nao conseguimos confirmar seu WhatsApp no Glow Up Connect.</p>
            <p>Verifique o link mais recente no WhatsApp ou e-mail de confirmacao ou solicite uma nova confirmacao pelo app.</p>
            <p>Se o problema persistir, confira se a mensagem foi enviada do mesmo numero cadastrado no perfil.</p>
            """;

    public static string CriarFalhaConfirmacaoGenerica() =>
        """
            <p>Nao conseguimos confirmar seu WhatsApp no Glow Up Connect.</p>
            <p>Verifique se o telefone esta cadastrado no perfil e use o link mais recente enviado por WhatsApp ou e-mail.</p>
            """;

    public static string CriarConfirmacaoSucesso(string nome, string telefonePerfil) =>
        $"""
            <p>Ola {Html(nome)},</p>
            <p>Seu WhatsApp <strong>{Html(telefonePerfil)}</strong> foi confirmado no Glow Up Connect.</p>
            <p>Voce passara a receber alertas de agendamento por este numero.</p>
            """;

    public static string CriarConfirmacaoJaRealizada(string nome) =>
        $"""
            <p>Ola {Html(nome)},</p>
            <p>Seu WhatsApp ja esta confirmado no Glow Up Connect.</p>
            <p>Nao e necessario enviar uma nova mensagem de confirmacao.</p>
            """;

    private const string CorAmarela = "#ffbf00";
    private const string CorEscura = "#282828";
    private const string CorTextoSuave = "rgba(40,40,40,0.6)";
    private const string CorWhatsApp = "#25d366";
    private const string CorLink = "#524cbc";
    private const string CorAlerta = "#cf3f3f";
    private const string CorSeguranca = "#71976c";
    private const string CorBordaSeguranca = "rgba(57,136,47,0.6)";

    public static string Criar(
        string nome,
        string telefonePerfil,
        string linkConfirmacao,
        string linkWhatsApp)
    {
        var nomeSeguro = Html(nome);
        var telefoneSeguro = Html(telefonePerfil);
        var linkConfirmacaoSeguro = Html(linkConfirmacao);
        var linkWhatsAppSeguro = Html(linkWhatsApp);
        var logoSrc = EmailTemplateInlineAssets.LogoSrc;
        var clockSrc = EmailTemplateInlineAssets.ClockSrc;
        var warningSrc = EmailTemplateInlineAssets.WarningSrc;
        var anoAtual = DateTime.UtcNow.Year;

        return $$"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="x-apple-disable-message-reformatting">
              <title>Confirme seu WhatsApp - GlowUp Connect</title>
              <style>
                @media only screen and (max-width: 620px) {
                  .email-shell { width: 100% !important; }
                  .email-card { width: 100% !important; border-radius: 0 !important; }
                  .content-pad { padding: 24px 20px !important; }
                  .title { font-size: 32px !important; line-height: 38px !important; }
                  .confirm-code { font-size: 28px !important; letter-spacing: 6px !important; }
                  .button { display: block !important; width: 100% !important; box-sizing: border-box !important; }
                  .header-logo { width: 96px !important; height: 96px !important; }
                }
              </style>
            </head>
            <body style="margin:0; padding:0; background-color:#ffffff; font-family:'Poppins', Arial, Helvetica, sans-serif; color:{{CorEscura}};">
              <div style="display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;">
                Confirme seu WhatsApp no GlowUp Connect.
              </div>

              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:#ffffff; margin:0; padding:24px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" class="email-shell" width="600" cellspacing="0" cellpadding="0" border="0" style="width:600px; max-width:600px;">
                      <tr>
                        <td class="email-card" style="background:#ffffff; border:1px solid rgba(40,40,40,0.4); border-radius:20px; overflow:hidden;">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                              <td style="background-color:{{CorAmarela}}; padding:0;">
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
                                <h1 class="title" style="margin:0; color:{{CorEscura}}; font-family:'Montserrat', Arial, Helvetica, sans-serif; font-size:48px; line-height:52px; font-weight:300; text-align:center;">
                                  Confirme seu WhatsApp
                                </h1>
                                <p style="margin:16px 0 0; color:{{CorTextoSuave}}; font-size:18px; line-height:26px; text-align:center;">
                                  Ola {{nomeSeguro}} &#128075;, confirme o numero <strong>{{telefoneSeguro}}</strong> para receber alertas de agendamento.
                                </p>
                              </td>
                            </tr>

                            <tr>
                              <td class="content-pad" style="padding:12px 32px 32px;">
                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background:rgba(255,255,255,0.6); border:0.5px solid rgba(40,40,40,0.6); border-radius:20px;">
                                  <tr>
                                    <td style="padding:32px 28px;">
                                      <p style="margin:0 0 24px; color:{{CorEscura}}; font-size:18px; line-height:26px; text-align:center;">
                                        Toque no botao abaixo para abrir a pagina de confirmacao. Em seguida, envie a mensagem pelo WhatsApp usando o numero cadastrado no perfil.
                                      </p>

                                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                        <tr>
                                          <td align="center" style="padding:0 0 20px;">
                                            <a class="button" href="{{linkConfirmacaoSeguro}}" target="_blank" style="display:inline-block; background:{{CorLink}}; color:#ffffff; text-decoration:none; font-size:18px; font-weight:700; padding:14px 32px; border-radius:12px; min-width:220px; text-align:center;">
                                              Abrir confirmacao
                                            </a>
                                          </td>
                                        </tr>
                                      </table>

                                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin:0 0 20px;">
                                        <tr>
                                          <td width="42%" style="border-top:1px solid rgba(40,40,40,0.25); font-size:0; line-height:0;">&nbsp;</td>
                                          <td align="center" style="padding:0 12px; color:{{CorTextoSuave}}; font-size:18px; line-height:18px; opacity:0.4; white-space:nowrap;">ou</td>
                                          <td width="42%" style="border-top:1px solid rgba(40,40,40,0.25); font-size:0; line-height:0;">&nbsp;</td>
                                        </tr>
                                      </table>

                                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                        <tr>
                                          <td align="center" style="padding:0 0 20px;">
                                            <a class="button" href="{{linkWhatsAppSeguro}}" target="_blank" style="display:inline-block; background:{{CorWhatsApp}}; color:#ffffff; text-decoration:none; font-size:18px; font-weight:700; padding:14px 32px; border-radius:12px; min-width:220px; text-align:center;">
                                              Confirmar no WhatsApp
                                            </a>
                                          </td>
                                        </tr>
                                      </table>

                                      <p style="margin:0; color:{{CorTextoSuave}}; font-size:12px; line-height:19px; text-align:center;">
                                        Se os botoes nao funcionarem, copie e abra este link no celular: {{linkConfirmacaoSeguro}}
                                      </p>

                                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin-top:24px;">
                                        <tr>
                                          <td style="padding:16px 18px; border:0.5px solid {{CorBordaSeguranca}}; border-radius:12px;">
                                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                              <tr>
                                                <td width="28" valign="top" style="padding-top:2px;">
                                                  <img src="{{warningSrc}}" alt="" width="16" height="16" style="display:block; width:16px; height:16px; border:0;">
                                                </td>
                                                <td valign="top">
                                                  <p style="margin:0; color:{{CorSeguranca}}; font-size:14px; line-height:20px; font-weight:700;">Aviso de seguranca</p>
                                                  <p style="margin:6px 0 0; color:{{CorSeguranca}}; font-size:12px; line-height:18px; font-weight:300;">
                                                    Nunca compartilhe este e-mail ou link. A equipe GlowUp Connect nunca pedira a sua senha.
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

                            <tr>
                              <td style="height:1px; background:rgba(40,40,40,0.15);"></td>
                            </tr>

                            <tr>
                              <td class="content-pad" style="padding:28px 40px 36px;">
                                <p style="margin:0 0 12px; color:{{CorEscura}}; font-size:18px; line-height:24px; font-weight:600;">Precisa de ajuda?</p>
                                <p style="margin:0 0 12px; color:{{CorTextoSuave}}; font-size:12px; line-height:20px;">
                                  Nosso suporte pode ajudar com cadastro, acesso e confirmacao de conta.
                                </p>
                                <p style="margin:0 0 8px; color:{{CorTextoSuave}}; font-size:12px; line-height:20px;">
                                  E-mail: <a href="mailto:suporte@glowupconnect.com" style="color:{{CorLink}}; text-decoration:none;">suporte@glowupconnect.com</a>
                                </p>
                                <p style="margin:0; color:{{CorTextoSuave}}; font-size:12px; line-height:20px;">
                                  Atendimento: segunda a sexta, das 9h as 18h.
                                </p>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <tr>
                        <td align="center" style="padding:18px 20px 0;">
                          <p style="margin:0; color:{{CorTextoSuave}}; font-size:11px; line-height:18px;">
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
