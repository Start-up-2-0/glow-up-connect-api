using System.Text.Json;
using GLOWAPI.Application.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class EvolutionWebhookParserTests
{
    [Fact]
    public void ExtrairTelefoneRemetente_DeveUsarRemoteJidPadrao()
    {
        var payload = JsonDocument.Parse("""
            {
              "data": {
                "key": {
                  "remoteJid": "5511988887777@s.whatsapp.net",
                  "fromMe": false
                }
              }
            }
            """).RootElement;

        var telefone = EvolutionWebhookParser.ExtrairTelefoneRemetente(payload);

        Assert.Equal("5511988887777", telefone);
    }

    [Fact]
    public void ExtrairTelefoneRemetente_DeveResolverLidComRemoteJidAlt()
    {
        var payload = JsonDocument.Parse("""
            {
              "data": {
                "key": {
                  "remoteJid": "69385314111689@lid",
                  "remoteJidAlt": "5511988887777@s.whatsapp.net",
                  "fromMe": false
                }
              }
            }
            """).RootElement;

        var telefone = EvolutionWebhookParser.ExtrairTelefoneRemetente(payload);

        Assert.Equal("5511988887777", telefone);
    }

    [Fact]
    public void ExtrairTelefoneRemetente_DeveResolverLidComSenderPn()
    {
        var payload = JsonDocument.Parse("""
            {
              "data": {
                "key": {
                  "remoteJid": "69385314111689@lid",
                  "senderPn": "5511988887777@s.whatsapp.net",
                  "fromMe": false
                }
              }
            }
            """).RootElement;

        var telefone = EvolutionWebhookParser.ExtrairTelefoneRemetente(payload);

        Assert.Equal("5511988887777", telefone);
    }

    [Fact]
    public void ExtrairTelefoneRemetente_LidFromMeFalse_NaoDeveUsarSenderDaInstancia()
    {
        var payload = JsonDocument.Parse("""
            {
              "data": {
                "key": {
                  "remoteJid": "60348602310753@lid",
                  "fromMe": false
                },
                "message": {
                  "extendedTextMessage": {
                    "text": "GLOW 691617"
                  }
                }
              },
              "sender": "557991917634@s.whatsapp.net"
            }
            """).RootElement;

        var telefone = EvolutionWebhookParser.ExtrairTelefoneRemetente(payload);

        Assert.Equal(string.Empty, telefone);
        Assert.False(EvolutionWebhookParser.IsMensagemInboundDoUsuario(payload));
    }

    [Fact]
    public void ExtrairTelefoneRemetente_LidFromMeTrue_DeveUsarSenderDoPayload()
    {
        var payload = JsonDocument.Parse("""
            {
              "data": {
                "key": {
                  "remoteJid": "69385314111689@lid",
                  "fromMe": true
                }
              },
              "sender": "5579991917634@s.whatsapp.net"
            }
            """).RootElement;

        var telefone = EvolutionWebhookParser.ExtrairTelefoneRemetente(payload);

        Assert.Equal("5579991917634", telefone);
    }

    [Fact]
    public void ExtrairTelefoneRemetente_LidComSenderEvolutionStaging_DeveExtrairTelefone()
    {
        var payload = JsonDocument.Parse("""
            {
              "event": "messages.upsert",
              "instance": "glowuphml",
              "data": {
                "key": {
                  "remoteJid": "67268163698795@lid",
                  "fromMe": true
                },
                "message": {
                  "conversation": "GLOW 484571"
                }
              },
              "sender": "557991917634@s.whatsapp.net"
            }
            """).RootElement;

        var telefone = EvolutionWebhookParser.ExtrairTelefoneRemetente(payload);

        Assert.Equal("557991917634", telefone);
        Assert.True(EvolutionWebhookParser.IsMensagemInboundDoUsuario(payload));
        Assert.Equal("GLOW 484571", EvolutionWebhookParser.ExtrairTextoMensagem(payload));
    }

    [Fact]
    public void ExtrairTelefoneRemetente_DeveUsarSenderSomenteSemDataKey()
    {
        var payload = JsonDocument.Parse("""
            {
              "sender": "5511988887777@s.whatsapp.net"
            }
            """).RootElement;

        var telefone = EvolutionWebhookParser.ExtrairTelefoneRemetente(payload);

        Assert.Equal("5511988887777", telefone);
    }

    [Fact]
    public void DescreverMotivoNaoInbound_DeveRetornarSemCampoData()
    {
        var payload = JsonDocument.Parse("""{ "event": "messages.upsert" }""").RootElement;

        Assert.Equal("sem_campo_data", EvolutionWebhookParser.DescreverMotivoNaoInbound(payload));
    }

    [Fact]
    public void DescreverMotivoNaoInbound_DeveRetornarTelefoneNaoExtraido()
    {
        var payload = JsonDocument.Parse("""
            {
              "data": {
                "key": {
                  "remoteJid": "69385314111689@lid",
                  "fromMe": false
                }
              }
            }
            """).RootElement;

        Assert.Equal("telefone_nao_extraido", EvolutionWebhookParser.DescreverMotivoNaoInbound(payload));
    }

    [Fact]
    public void IsMensagemInboundDoUsuario_DeveAceitarFromMeTrue()
    {
        var payload = JsonDocument.Parse("""
            {
              "data": {
                "key": {
                  "remoteJid": "5511988887777@s.whatsapp.net",
                  "fromMe": true
                }
              }
            }
            """).RootElement;

        Assert.True(EvolutionWebhookParser.IsMensagemInboundDoUsuario(payload));
    }

    [Fact]
    public void ExtrairRemoteJidConversa_DeveRetornarRemoteJidDoPayload()
    {
        var payload = JsonDocument.Parse("""
            {
              "data": {
                "key": {
                  "remoteJid": "60348602310753@lid",
                  "fromMe": false
                }
              }
            }
            """).RootElement;

        Assert.Equal("60348602310753@lid", EvolutionWebhookParser.ExtrairRemoteJidConversa(payload));
        Assert.True(EvolutionWebhookParser.EhRemoteJidLid("60348602310753@lid"));
    }

    [Fact]
    public void ExtrairTextoMensagem_DeveLerEphemeralMessage()
    {
        var payload = JsonDocument.Parse("""
            {
              "data": {
                "message": {
                  "ephemeralMessage": {
                    "message": {
                      "conversation": "GLOW 482913"
                    }
                  }
                }
              }
            }
            """).RootElement;

        var texto = EvolutionWebhookParser.ExtrairTextoMensagem(payload);

        Assert.Equal("GLOW 482913", texto);
    }

    [Fact]
    public void ExtrairTextoMensagem_DeveLerExtendedTextMessage()
    {
        var payload = JsonDocument.Parse("""
            {
              "data": {
                "message": {
                  "extendedTextMessage": {
                    "text": "GLOW 482913"
                  }
                }
              }
            }
            """).RootElement;

        var texto = EvolutionWebhookParser.ExtrairTextoMensagem(payload);

        Assert.Equal("GLOW 482913", texto);
    }
}
