using System.Text.Json;

namespace GLOWAPI.Application.Helpers;

public static class EvolutionWebhookParser
{
    public static string? ExtrairEvento(JsonElement payload) =>
        payload.TryGetProperty("event", out var evento) ? evento.GetString() : null;

    public static string? ExtrairInstancia(JsonElement payload) =>
        payload.TryGetProperty("instance", out var instancia) ? instancia.GetString() : null;

    public static bool? ExtrairFromMe(JsonElement payload)
    {
        if (!payload.TryGetProperty("data", out var data)
            || !data.TryGetProperty("key", out var key)
            || !key.TryGetProperty("fromMe", out var fromMe))
        {
            return null;
        }

        return fromMe.ValueKind == JsonValueKind.True;
    }

    public static string DescreverMotivoNaoInbound(JsonElement payload)
    {
        if (!payload.TryGetProperty("data", out _))
        {
            return "sem_campo_data";
        }

        if (string.IsNullOrWhiteSpace(ExtrairTelefoneRemetente(payload)))
        {
            return "telefone_nao_extraido";
        }

        return "desconhecido";
    }

    public static bool IsMensagemInboundDoUsuario(JsonElement payload)
    {
        if (!payload.TryGetProperty("data", out _))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(ExtrairTelefoneRemetente(payload));
    }

    public static string ExtrairTelefoneRemetente(JsonElement payload)
    {
        if (payload.TryGetProperty("data", out var data)
            && data.TryGetProperty("key", out var key))
        {
            var telefone = ResolverTelefoneDeKey(key);
            if (!string.IsNullOrWhiteSpace(telefone))
            {
                return telefone;
            }

            if (key.TryGetProperty("fromMe", out var fromMe)
                && fromMe.ValueKind == JsonValueKind.True
                && payload.TryGetProperty("sender", out var senderFromMe))
            {
                var telefoneSender = NormalizarJid(senderFromMe.GetString());
                if (!string.IsNullOrWhiteSpace(telefoneSender))
                {
                    return telefoneSender;
                }
            }

            return string.Empty;
        }

        if (payload.TryGetProperty("sender", out var sender))
        {
            var telefoneSender = NormalizarJid(sender.GetString());
            if (!string.IsNullOrWhiteSpace(telefoneSender))
            {
                return telefoneSender;
            }
        }

        return string.Empty;
    }

    public static string? ExtrairRemoteJidConversa(JsonElement payload)
    {
        if (payload.TryGetProperty("data", out var data)
            && data.TryGetProperty("key", out var key)
            && key.TryGetProperty("remoteJid", out var remoteJid))
        {
            var jid = remoteJid.GetString();
            return string.IsNullOrWhiteSpace(jid) ? null : jid;
        }

        return null;
    }

    public static bool EhRemoteJidLid(string? remoteJid) =>
        !string.IsNullOrWhiteSpace(remoteJid)
        && remoteJid.Contains("@lid", StringComparison.OrdinalIgnoreCase);

    public static string ExtrairTextoMensagem(JsonElement payload)
    {
        if (!payload.TryGetProperty("data", out var data)
            || !data.TryGetProperty("message", out var message))
        {
            return string.Empty;
        }

        return ExtrairTextoDeMessageObject(message);
    }

    private static string ResolverTelefoneDeKey(JsonElement key)
    {
        if (key.TryGetProperty("remoteJid", out var remoteJid))
        {
            var jid = remoteJid.GetString() ?? string.Empty;
            if (jid.Contains("@lid", StringComparison.OrdinalIgnoreCase))
            {
                if (key.TryGetProperty("remoteJidAlt", out var remoteJidAlt))
                {
                    var telefoneAlt = NormalizarJid(remoteJidAlt.GetString());
                    if (!string.IsNullOrWhiteSpace(telefoneAlt))
                    {
                        return telefoneAlt;
                    }
                }

                if (key.TryGetProperty("senderPn", out var senderPn))
                {
                    var telefonePn = NormalizarJid(senderPn.GetString());
                    if (!string.IsNullOrWhiteSpace(telefonePn))
                    {
                        return telefonePn;
                    }
                }

                return string.Empty;
            }

            return NormalizarJid(jid);
        }

        return string.Empty;
    }

    private static string NormalizarJid(string? jid)
    {
        if (string.IsNullOrWhiteSpace(jid))
        {
            return string.Empty;
        }

        var prefixo = jid.Split('@')[0];
        return TelefoneHelper.NormalizarParaWhatsApp(prefixo);
    }

    private static string ExtrairTextoDeMessageObject(JsonElement message)
    {
        if (message.TryGetProperty("conversation", out var conversation))
        {
            var texto = conversation.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(texto))
            {
                return texto;
            }
        }

        if (message.TryGetProperty("extendedTextMessage", out var extended)
            && extended.TryGetProperty("text", out var text))
        {
            var texto = text.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(texto))
            {
                return texto;
            }
        }

        if (message.TryGetProperty("buttonsResponseMessage", out var buttonResponse)
            && buttonResponse.TryGetProperty("selectedDisplayText", out var selected))
        {
            var texto = selected.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(texto))
            {
                return texto;
            }
        }

        if (message.TryGetProperty("ephemeralMessage", out var ephemeral)
            && ephemeral.TryGetProperty("message", out var ephemeralMessage))
        {
            var texto = ExtrairTextoDeMessageObject(ephemeralMessage);
            if (!string.IsNullOrWhiteSpace(texto))
            {
                return texto;
            }
        }

        return string.Empty;
    }
}
