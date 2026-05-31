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
    private const string CorAmarela = "#ffbf00";
    private const string CorEscura = "#282828";
    private const string CorTextoSuave = "rgba(40,40,40,0.6)";
    private const string CorLink = "#524cbc";
    private const string CorAlerta = "#cf3f3f";
    private const string CorSeguranca = "#71976c";
    private const string CorBordaSeguranca = "rgba(57,136,47,0.6)";

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
        var logoDataUri = EmailTemplateAssets.LogoDataUri;
        var warningDataUri = EmailTemplateAssets.WarningDataUri;
        var anoAtual = DateTime.UtcNow.Year;

        return $$"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="x-apple-disable-message-reformatting">
              <title>Confirme seu e-mail - GlowUp Connect</title>
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
            <body style="margin:0; padding:0; background-color:#ffffff; font-family:'Poppins', Arial, Helvetica, sans-serif; color:{{CorEscura}};">
              <div style="display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;">
                Confirme seu cadastro no GlowUp Connect com o codigo {{codigoSeguro}}.
              </div>

              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:#ffffff; margin:0; padding:24px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" class="email-shell" width="600" cellspacing="0" cellpadding="0" border="0" style="width:600px; max-width:600px;">
                      <tr>
                        <td class="email-card" style="background:#ffffff; border:1px solid rgba(40,40,40,0.4); border-radius:20px; overflow:hidden;">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                            <tr>
                              <td class="header-banner" style="background-color:{{CorAmarela}}; padding:0; position:relative;">
                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                  <tr>
                                    <td width="130" valign="middle" style="padding:12px 0 12px 8px;">
                                      <img class="header-logo" src="{{logoDataUri}}" alt="GlowUp Connect" width="120" height="120" style="display:block; width:120px; height:120px; border:0;">
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
                                  Confirme seu e-mail
                                </h1>
                                <p style="margin:16px 0 0; color:{{CorTextoSuave}}; font-size:18px; line-height:26px; text-align:center;">
                                  Ola {{nomeSeguro}} &#128075;, finalize o seu cadastro no GlowUp Connect para acessar sua conta com seguranca.
                                </p>
                              </td>
                            </tr>

                            <tr>
                              <td class="content-pad" style="padding:12px 32px 32px;">
                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background:rgba(255,255,255,0.6); border:0.5px solid rgba(40,40,40,0.6); border-radius:20px;">
                                  <tr>
                                    <td style="padding:32px 28px;">
                                      <p style="margin:0 0 24px; color:{{CorEscura}}; font-size:18px; line-height:26px; text-align:center;">
                                        Use o botao abaixo para confirmar automaticamente ou copie o codigo de confirmacao.
                                      </p>

                                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                        <tr>
                                          <td align="center" style="padding:0 0 20px;">
                                            <a class="button" href="{{linkSeguro}}" target="_blank" style="display:inline-block; background:{{CorEscura}}; color:{{CorAmarela}}; text-decoration:none; font-size:18px; font-weight:700; padding:14px 32px; border-radius:12px; min-width:220px; text-align:center;">
                                              Confirmar e-mail
                                            </a>
                                          </td>
                                        </tr>
                                      </table>

                                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin:0 0 24px;">
                                        <tr>
                                          <td width="42%" style="border-top:1px solid rgba(40,40,40,0.25); font-size:0; line-height:0;">&nbsp;</td>
                                          <td align="center" style="padding:0 12px; color:{{CorTextoSuave}}; font-size:18px; line-height:18px; opacity:0.4; white-space:nowrap;">ou</td>
                                          <td width="42%" style="border-top:1px solid rgba(40,40,40,0.25); font-size:0; line-height:0;">&nbsp;</td>
                                        </tr>
                                      </table>

                                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin-bottom:20px;">
                                        <tr>
                                          <td align="center" style="padding:22px 16px; background:{{CorEscura}}; border:2px dashed {{CorAmarela}}; border-radius:20px;">
                                            <div style="color:{{CorAmarela}}; font-size:14px; font-weight:300; letter-spacing:0.04em; text-transform:uppercase;">
                                              Codigo de confirmacao
                                            </div>
                                            <div class="confirm-code" style="margin-top:10px; color:{{CorAmarela}}; font-size:32px; line-height:40px; font-weight:500; letter-spacing:7px;">
                                              {{codigoSeguro}}
                                            </div>
                                          </td>
                                        </tr>
                                      </table>

                                      {{estadoVisual}}

                                      <p style="margin:20px 0 8px; color:{{CorTextoSuave}}; font-size:14px; line-height:22px; text-align:center;">
                                        Se o botao nao funcionar, copie e cole este link no navegador:
                                      </p>
                                      <p style="margin:0; text-align:center;">
                                        <a href="{{linkSeguro}}" target="_blank" style="color:{{CorLink}}; font-size:14px; line-height:22px; text-decoration:underline; word-break:break-all;">{{linkSeguro}}</a>
                                      </p>

                                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin-top:24px;">
                                        <tr>
                                          <td style="padding:16px 18px; border:0.5px solid {{CorBordaSeguranca}}; border-radius:12px;">
                                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                              <tr>
                                                <td width="28" valign="top" style="padding-top:2px;">
                                                  <img src="{{warningDataUri}}" alt="" width="16" height="16" style="display:block; width:16px; height:16px; border:0;">
                                                </td>
                                                <td valign="top">
                                                  <p style="margin:0; color:{{CorSeguranca}}; font-size:14px; line-height:20px; font-weight:700;">Aviso de seguranca</p>
                                                  <p style="margin:6px 0 0; color:{{CorSeguranca}}; font-size:12px; line-height:18px; font-weight:300;">
                                                    Nunca compartilhe este e-mail, link ou codigo. A equipe GlowUp Connect nunca pedira a sua senha.
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
                              <td style="height:1px; background:rgba(40,40,40,0.15); margin:0 32px;"></td>
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

    private static string ObterEstadoVisual(ConfirmacaoEmailEstado estado, string validadeTexto) =>
        estado switch
        {
            ConfirmacaoEmailEstado.CodigoExpirado => CriarEstadoExpiracao(
                "codigo-expirado",
                "Codigo expirado",
                "Solicite um novo codigo para concluir sua confirmacao com seguranca."),
            ConfirmacaoEmailEstado.CodigoInvalido => CriarEstadoExpiracao(
                "codigo-invalido",
                "Codigo invalido",
                "Confira os numeros digitados ou use o botao de confirmacao deste e-mail."),
            ConfirmacaoEmailEstado.ConfirmacaoRealizada => CriarEstadoSucesso(
                "Confirmacao realizada com sucesso",
                "Seu e-mail foi confirmado. Agora voce ja pode acessar sua conta."),
            _ => CriarEstadoExpiracao(
                "codigo-ativo",
                $"Este codigo expira em <strong>{Html(validadeTexto)}</strong>",
                string.Empty,
                incluirIconeRelogio: true,
                apenasMensagemPrincipal: true)
        };

    private static string CriarEstadoExpiracao(
        string estadoId,
        string titulo,
        string descricao,
        bool incluirIconeRelogio = false,
        bool apenasMensagemPrincipal = false)
    {
        var clockDataUri = EmailTemplateAssets.ClockDataUri;
        var icone = incluirIconeRelogio
            ? $"""<img src="{clockDataUri}" alt="" width="20" height="20" style="display:block; width:20px; height:20px; border:0;">"""
            : string.Empty;

        var descricaoHtml = string.IsNullOrEmpty(descricao)
            ? string.Empty
            : $"""<p style="margin:4px 0 0; color:{CorAlerta}; font-size:12px; line-height:18px;">{Html(descricao)}</p>""";

        var tituloHtml = apenasMensagemPrincipal
            ? $"""<p style="margin:0; color:{CorAlerta}; font-size:12px; line-height:18px;">{titulo}</p>"""
            : $"""<p style="margin:0; color:{CorAlerta}; font-size:12px; line-height:18px; font-weight:700;">{Html(titulo)}</p>{descricaoHtml}""";

        return $"""
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" data-confirmation-state="{estadoId}" style="margin-bottom:20px;">
          <tr>
            <td style="padding:12px 14px; border:0.5px solid {CorAlerta}; border-radius:12px;">
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

    private static string CriarEstadoSucesso(string titulo, string descricao) =>
        $"""
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" data-confirmation-state="confirmacao-realizada" style="margin-bottom:20px;">
          <tr>
            <td style="padding:12px 14px; border:0.5px solid {CorBordaSeguranca}; border-radius:12px;">
              <p style="margin:0; color:{CorSeguranca}; font-size:12px; line-height:18px; font-weight:700;">{Html(titulo)}</p>
              <p style="margin:4px 0 0; color:{CorSeguranca}; font-size:12px; line-height:18px;">{Html(descricao)}</p>
            </td>
          </tr>
        </table>
        """;

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
